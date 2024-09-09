
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aria2Fast.Service.Model;
using Aria2NET;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Net.Http;
using System.Net;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Flurl.Http;
using System.Linq;
using System.Timers;
using Aria2Fast.Utils;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;
using System.Text;
using System.Collections.Concurrent;

namespace Aria2Fast.Service
{
    public enum LinkStatus
    {
        Linking,
        Error,
        Success
    }

    public class Aria2ApiManager
    {
        public const string KARIA2_STATUS_ACTIVE = "active";
        public const string KARIA2_STATUS_WAITING = "waiting";
        public const string KARIA2_STATUS_PAUSED = "paused";
        public const string KARIA2_STATUS_ERROR = "error";
        public const string KARIA2_STATUS_COMPLETE = "complete";
        public const string KARIA2_STATUS_REMOVED = "removed";


        public IObservable<Aria2Event> EventReceived => _eventReceivedSubject.AsObservable();
        public bool Connected { set; get; }
        public string CurrentRpc { set; get; }


        private static int kMaxTaskListCount = 100;
        private static Aria2ApiManager instance = new Aria2ApiManager();
        private Aria2NetClient _client ;
        private Timer _debounceTimer;
        private readonly object _locker = new object();
        private static ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();
        private readonly Subject<Aria2Event> _eventReceivedSubject = new();
        private Process? _aria2Process = null;
        private static object _lockForUpdateTask = new object();

        public static Aria2ApiManager Instance
        {
            get
            {

                return instance;
            }
        }

        public ObservableCollection<TaskModel> TaskList { set; get; } = new ObservableCollection<TaskModel>();


        Aria2ApiManager()
        {

        }

        public void Init()
        {
            try
            {
                if (AppConfig.Instance.ConfigData.Aria2UseLocal)
                {
                    //启动本地Aria2
                    StartupLocalAria2();
                }
                else
                {
                    //无需操作
                }
                UpdateRpcAndTest();
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Info($"启动本地Aria2Fast失败：{ex}");
               
            }
            SetupEvent();
        }

        private async Task<bool> UpdateTrackersList()
        {
            try
            {
                var aria2Path = Path.Combine(Directory.GetCurrentDirectory(), "Aria2");
                var aria2Conf = Path.Combine(aria2Path, "aria2.conf");
                //https://cdn.jsdelivr.net/gh/ngosang/trackerslist@master/trackers_all.txt
                //https://cf.trackerslist.com/best.txt

                string list = await "https://cdn.jsdelivr.net/gh/ngosang/trackerslist@master/trackers_all.txt".WithTimeout(10).GetStringAsync();
                list = list.Replace("\n\n", ",");

                //更新内容到
                var lines = File.ReadAllLines(aria2Conf);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("bt-tracker="))
                    {
                        lines[i] = $"bt-tracker={list}";
                    }
                }

                File.WriteAllLines(aria2Conf, lines);
                Debug.WriteLine(list);
                return true;
            }
            catch (Exception e) 
            {

            }

            
            return false;
        }




        /// <summary>
        /// 启动本地Aria2并管理，退出时跟随退出
        /// </summary>
        private async void StartupLocalAria2()
        {
            //key=ARIA2FAST
            //port=6809
            //StopLocalAria2();

            var aria2Path = Path.Combine(Directory.GetCurrentDirectory(), "Aria2");
            var aria2File = Path.Combine(aria2Path, "aria2c.exe");
            var aria2Conf = Path.Combine(aria2Path, "aria2.conf");

            EasyLogManager.Logger.Info(aria2Path);
            EasyLogManager.Logger.Info(aria2File);
            EasyLogManager.Logger.Info(aria2Conf);

            var firstRun = false;
            if (File.Exists(aria2File))
            {
                if (!File.Exists(aria2Conf))
                {
                    EasyLogManager.Logger.Info($"本地Aria2写出配置");
                    firstRun = true;
                    //写出配置
                    PathHelper.WriteResourceToFile("Aria2Fast.Assets.Config.aria2.conf", aria2Conf);
                    PathHelper.WriteResourceToFile("Aria2Fast.Assets.Config.dht.dat", Path.Combine(aria2Path, "dht.dat"));
                    PathHelper.WriteResourceToFile("Aria2Fast.Assets.Config.dht6.dat", Path.Combine(aria2Path, "dht6.dat"));
                    File.WriteAllBytes(Path.Combine(aria2Path, "aria2.session"), new byte[0]);
                }

                await UpdateTrackersList();

                //如果没配置，则写出配置，否则读取配置中存储路径
                var conf = new ConfigFileManager(aria2Conf);
                var port = conf.GetValue("rpc-listen-port");
                var secret = conf.GetValue("rpc-secret");
                var dir = conf.GetValue("dir");

                if (!Directory.Exists(dir)) {
                    try
                    {
                        Directory.CreateDirectory(dir);
                    }
                    catch (Exception ex) 
                    {
                        //D盘没有？C盘总有！
                        dir = "C:\\downloads";
                    }
                }

                var rpc = $"http://127.0.0.1:{port}/jsonrpc";
                AppConfig.Instance.ConfigData.Aria2RpcLocal = rpc;
                AppConfig.Instance.ConfigData.Aria2TokenLocal = secret;

                if (firstRun || !AppConfig.Instance.ConfigData.AddTaskSavePathList.ContainsKey(AppConfig.Instance.ConfigData.Aria2RpcAuto))
                {
                    AppConfig.Instance.ConfigData.Aria2LocalSavePath = dir;
                    AppConfig.Instance.InitLocalPath(dir);
                }

                EasyLogManager.Logger.Info($"本地Aria2：{rpc}");
                EasyLogManager.Logger.Info($"本地Aria2下载路径：{dir}");

                if (!IsLocalAria2Runing())
                {
                    EasyLogManager.Logger.Info($"准备启动Aria2：{aria2File} 目录：{aria2Path}");
                    //本地的Aria2已经在运行了，这里不再启动
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = $"{aria2File}",
                        Arguments = $"--conf-path=\"{aria2Conf}\"",
                        WorkingDirectory = aria2Path,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8, // 设置标准输出编码
                        StandardErrorEncoding = Encoding.UTF8   // 设置标准错误编码
                    };
          
                    _aria2Process = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };

                    _aria2Process.OutputDataReceived += (sender, e) => { if (e.Data != null) AddToQueue(e.Data); };
                    _aria2Process.ErrorDataReceived += (sender, e) => { if (e.Data != null) AddToQueue(e.Data); };

                    _aria2Process.Start();
                    _aria2Process.BeginOutputReadLine();
                    _aria2Process.BeginErrorReadLine();

                    // 等待 5 秒（异步）
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        UpdateRpcAndTest();
                    });

                    
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        RecordOutput();
                    });

                    EasyLogManager.Logger.Info($"Aria2已启动");
                } 
                else
                {
                    EasyLogManager.Logger.Info($"Aria2已启动：{aria2File}");
                }
                //启动进程

            }
            else
            {
                EasyLogManager.Logger.Error("本地Aria2不存在！");
            }
        }

        private static void AddToQueue(string line)
        {
            _outputQueue.Enqueue(line);
            while (_outputQueue.Count > 1000)
            {
                _outputQueue.TryDequeue(out _);
            }
        }

        private static void RecordOutput()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in _outputQueue)
            {
                sb.AppendLine(line);
            }
            EasyLogManager.Logger.Info(@$"Aria2启动日志:{sb.ToString()}");
        }


        public static bool ExistLocalAria2()
        {
            var aria2Path = Path.Combine(Directory.GetCurrentDirectory(), "Aria2");
            var aria2File = Path.Combine(aria2Path, "aria2c.exe");

            if (File.Exists(aria2File))
            {
                return true;
            }
            return false;
        }



        public bool IsLocalAria2Runing()
        {
            foreach (var process in Process.GetProcessesByName("aria2c"))
            {
                try
                {
                    var aria2Path = Path.Combine(Directory.GetCurrentDirectory(), "Aria2");
                    var aria2File = Path.Combine(aria2Path, "aria2c.exe");
                    var path = process.MainModule.FileName;
                    if (aria2File == path)
                    {
                        return true;
                    }


                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex);
                    Debug.WriteLine($"Could not terminate process {process.Id}: {ex.Message}");
                }
            }
            return false;
        }

        


        public void UpdateLocalAria2()
        {
            if (AppConfig.Instance.ConfigData.Aria2UseLocal) 
            {
                StartupLocalAria2();
            } 
        }
        


        private void SetupEvent()
        {
            Task.Run(async () =>
            {

                while (true)
                {
                    try
                    {
                        if (Connected)
                        {
                            await UpdateTask();
                        }
                        
                    }
                    catch (Exception ex)
                    {

                    }

                    await Task.Delay(5000);


                }
            });
        }

        /// <summary>
        /// 从一个BT的URL添加到下载中（用于订阅的下载）
        /// </summary>
        /// <param name="url"></param>
        public async Task<Aria2FastDownloadResult> DownloadBtFileUrl(string url, string path, string taskName = "")
        {
            //先下载BT文件

            byte[] data = { };

            if (url.StartsWith("http"))
            {
                if (AppConfig.Instance.ConfigData.SubscriptionProxyOpen && 
                    !string.IsNullOrEmpty(AppConfig.Instance.ConfigData.SubscriptionProxy))
                {
                    var proxyUrl = AppConfig.Instance.ConfigData.SubscriptionProxy;
                    var proxy = new WebProxy(proxyUrl);
                    var handler = new HttpClientHandler() { Proxy = proxy };
                    var client = new HttpClient(handler);

                    // 注意这里的GET请求的地址需要替换为你需要请求的地址
                    var response = client.GetAsync(url).Result;
                    data = await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    data = await url.GetBytesAsync();
                }

                var config = new Dictionary<String, Object>
                {
                    { "dir", System.IO.Path.Combine(path, taskName)}
                };

                Aria2FastDownloadResult downloadResult = new Aria2FastDownloadResult();
                if (data.Length > 0)
                {
                    var result = await _client.AddTorrentAsync(data, options: config, position: 0);
                    Debug.WriteLine($"DownloadBtFileUrl结果：{result}");
                    downloadResult.IsSuccessed = IsGid(result);
                    downloadResult.Gid = result;
                }
                else
                {
                    downloadResult.IsSuccessed = false;
                    downloadResult.ErrorMessage = "无法下载BT文件";
                }

                return downloadResult;
            }
            else
            {
                var config = new Dictionary<String, Object>
                {
                    { "dir", System.IO.Path.Combine(path, taskName)}
                };

                Aria2FastDownloadResult downloadResult = new Aria2FastDownloadResult();
                var result = await _client.AddUriAsync(new List<string> { url }, options: config, position: 0);
                Debug.WriteLine($"DownloadBtFileUrl结果#2：{result}");

                await Task.Delay(1000);
                var statusResult = await _client.TellStatusAsync(result);
                if (statusResult.Bittorrent != null)
                {
                    Debug.WriteLine($"写入Hash：{statusResult.InfoHash}");
                    downloadResult.InfoHash = statusResult.InfoHash;
                }

                downloadResult.IsSuccessed = IsGid(result);
                downloadResult.Gid = result;

                return downloadResult;
            }

            
        }

        public async Task<Aria2FastDownloadResult> DownloadUrl(string url, string savePath = "")
        {
            var result = await _client.AddUriAsync(new List<String>
            {
                url
            },
            new Dictionary<String, Object>
            {
                            { "dir", savePath}
            }, 0);


            Debug.WriteLine($"DownloadBtFileUrl结果：{result}");

            Aria2FastDownloadResult downloadResult = new Aria2FastDownloadResult();
            downloadResult.IsSuccessed = IsGid(result);
            downloadResult.Gid = result;


            return downloadResult;
        }

        public async Task<Aria2FastDownloadResult> DownloadBtFile(string filePath, string savePath = "")
        {
            var config = new Dictionary<String, Object>
                    {
                       { "dir", savePath}
                    };

            var result = await _client.AddTorrentAsync(File.ReadAllBytes(filePath), options: config, position: 0);

            Debug.WriteLine($"DownloadBtFile结果：{result}");

            Aria2FastDownloadResult downloadResult = new Aria2FastDownloadResult();
            downloadResult.IsSuccessed = IsGid(result);
            downloadResult.Gid = result;


            return downloadResult;
        }


        /// <summary>
        /// 执行一次更新任务
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task<bool> UpdateTask()
        {
            var tasks = await _client.TellAllAsync();

            lock (_lockForUpdateTask)
            {                
                //检查是否有下载完成的数据
                if (tasks != null && tasks.Count() > 0)
                {
                    foreach (var task in TaskList)
                    {
                        var clearTask = tasks.Any(a =>
                        {
                            return a.Gid == task.Data.Gid &&
                                task.Data.Status != KARIA2_STATUS_COMPLETE &&
                                a.Status == KARIA2_STATUS_COMPLETE;
                        }
                        );

                        if (clearTask)
                        {
                            EasyLogManager.Logger.Info($"下载完成 {task.SubscriptionName}");
                        }
                        if (clearTask && AppConfig.Instance.ConfigData.PushDeerOpen)
                        {
                            PushDeer.SendPushDeer($"[{task.SubscriptionName}]下载完成");
                        }

                        _eventReceivedSubject.OnNext(new DownloadSuccessEvent(task.SubscriptionName));
                    }
                }


                MainWindow.Instance.Dispatcher.Invoke(() =>
                {
                    //TODO 更顺滑的更新任务
                    if (tasks.Count - TaskList.Count > 0)
                    {
                        while (tasks.Count - TaskList.Count > 0)
                        {
                            TaskList.Add(new TaskModel());
                        }
                    }
                    else if (tasks.Count - TaskList.Count < 0)
                    {
                        while (tasks.Count - TaskList.Count < 0)
                        {
                            TaskList.RemoveAt(TaskList.Count - 1);
                        }
                    }
                    tasks = tasks.OrderByDescending(a => a.Status == KARIA2_STATUS_PAUSED).ToArray();
                    tasks = tasks.OrderByDescending(a => a.Status == KARIA2_STATUS_WAITING).ToArray();
                    tasks = tasks.OrderByDescending(a => a.Status == KARIA2_STATUS_ACTIVE).ToArray();
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        TaskList[i].Data = tasks[i];
                    }
                });
            }
            
            return tasks.Count() > 0;
        }



        public async Task<bool> DeleteFile(string gid)
        {
            try
            {
                Debug.WriteLine($"删除任务：{gid}");
                var result = await _client.RemoveAsync(gid);
                return IsGid(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public async Task<bool> RemoveDownloadResult(string gid)
        {
            try
            {
                Debug.WriteLine($"删除下载结果：{gid}");
                await _client.RemoveDownloadResultAsync(gid);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        

        internal async Task<bool> UnpauseTask(string gid)
        {
            try
            {
                var result = await _client.UnpauseAsync(gid);
                return IsGid(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }

        }

        internal async Task<bool> PauseTask(string gid)
        {
            try
            {
                var result = await _client.PauseAsync(gid);
                return IsGid(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }

        }

        internal void UpdateRpcDebounce()
        {
            lock (_locker)
            {
                // Dispose previous timer
                _debounceTimer?.Dispose();

                // Create a new timer that delays for 1 second
                _debounceTimer = new Timer(2000);

                // After 1 second, execute the method
                _debounceTimer.Elapsed += (s, e) => UpdateRpcAndTest();
                _debounceTimer.AutoReset = false; // Make sure the timer runs only once

                _debounceTimer.Start();
            }
        }


        internal async Task<bool> UpdateRpcAndTest()
        {
            Debug.WriteLine("开始检查节点可用性");
            var rpc = AppConfig.Instance.ConfigData.Aria2RpcAuto;
            var token = AppConfig.Instance.ConfigData.Aria2TokenAuto;
            int retryCount = 0;

            while (retryCount < 3)
            {
                try
                {
                    var currentRpc = AppConfig.Instance.ConfigData.Aria2RpcAuto;
                    if (currentRpc != rpc)
                    {
                        return false;
                    }
                    _eventReceivedSubject.OnNext(new LoginStartEvent());
                    _client = new Aria2NetClient(rpc, token);

                    var result = await _client.TellAllAsync();

                    currentRpc = AppConfig.Instance.ConfigData.Aria2RpcAuto;
                    if (currentRpc != rpc)
                    {
                        return false;
                    }

                    if (result.Count >= 0)
                    {
                        Connected = true;
                        CurrentRpc = rpc;
                        _eventReceivedSubject.OnNext(new LoginResultEvent(true));
                        UpdateTask();
                        return true;
                    }
                    else
                    {
                        Connected = false;
                        CurrentRpc = rpc;
                        _eventReceivedSubject.OnNext(new LoginResultEvent(false));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    Connected = false;
                    CurrentRpc = rpc;
                    _eventReceivedSubject.OnNext(new LoginResultEvent(false));
                }

                retryCount++;
                await Task.Delay(3000);
            }

            return false;
        }

        private static bool IsGid(string gid)
        {
            if (string.IsNullOrWhiteSpace(gid))
            {
                return false;
            }
            //2089b05ecca3d829
            if (gid.Length == 16)
            {
                return true;
            }
            return false;
        }
    }
}
