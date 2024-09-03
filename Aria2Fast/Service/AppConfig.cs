using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Diagnostics;
using PropertyChanged;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Aria2Fast.Service.Model.SubscriptionModel;

namespace Aria2Fast.Service
{
    [AddINotifyPropertyChangedInterface]
    public class AppConfigData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, Action> _propertyChangedActions;

        public AppConfigData()
        {

        }

        public void Init()
        {
            _propertyChangedActions = new Dictionary<string, Action>
                {
                    { nameof(Aria2Rpc), async () => await Aria2ApiManager.Instance.UpdateRpcAndTest() },

                    { nameof(Aria2Token), async () => await Aria2ApiManager.Instance.UpdateRpcAndTest() },

                    { nameof(Aria2UseLocal), () => {

                        if (!Aria2ApiManager.ExistLocalAria2())
                        {
                            if (Aria2UseLocal == true)
                            {
                                Aria2UseLocal = false;
                            }
                            return;
                        }

                        Aria2ApiManager.Instance.UpdateLocalAria2();
                        Aria2ApiManager.Instance.UpdateRpcAndTest();

                    } }

                };
            this.PropertyChanged += AppConfigData_PropertyChanged;
        }

        private void AppConfigData_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender != null)
            {
                if (_propertyChangedActions.TryGetValue(e.PropertyName, out Action action))
                {
                    action();
                }
            }

        }

        /// <summary>
        /// 任务的保存路径
        /// </summary>
        public Dictionary<string, List<string>> AddTaskSavePathList { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// 订阅的保存路径
        /// </summary>
        public Dictionary<string, List<string>> AddSubscriptionSavePathList { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// 常用过滤器名称（不区分RPC了）
        /// </summary>
        public List<SubscriptionFilterModel> AddSubscriptionFilterList { get; set; } = new List<SubscriptionFilterModel>();

        //OSS相关设置
        public bool OSSSynchronizeOpen { get; set; } = false;

        public string OSSEndpoint { get; set; } = string.Empty;

        public string OSSBucket { get; set; } = string.Empty;

        public string OSSAccessKeyId { get; set; } = string.Empty;

        public string OSSAccessKeySecret { get; set; } = string.Empty;


        public bool PushDeerOpen { get; set; } = false;

        public string PushDeerKey { get; set; } = string.Empty;


        public bool SubscriptionProxyOpen { get; set; } = false;

        public string SubscriptionProxy { get; set; } = string.Empty;

        /// <summary>
        /// OpenAIKey 用于RSS为集中订阅时，使用OpenAI提取连接中的作品名称
        /// </summary>
        public string OpenAIKey { get; set; } = string.Empty;

        public string OpenAIProxy { get; set; } = string.Empty;

        public bool OpenAIOpen { get; set; } = false;


        /// <summary>
        /// 用于第三方转发服务的实现
        /// </summary>
        public string OpenAIHost { get; set; } = string.Empty;

        //当前客户端ID
        public string ClientId { get; set; } = string.Empty;

        //远程RPC
        public string Aria2Rpc { get; set; } = string.Empty;

        public string Aria2Token { get; set; } = string.Empty;

        //本地RPC
        public string Aria2RpcLocal { get; set; } = string.Empty;

        public string Aria2TokenLocal { get; set; } = string.Empty;


        public string Aria2RpcAuto
        {
            get
            {
                if (Aria2UseLocal)
                {
                    return Aria2RpcLocal;
                }
                return Aria2Rpc;
            }
        }

        public string Aria2TokenAuto
        {
            get
            {
                if (Aria2UseLocal)
                {
                    return Aria2TokenLocal;
                }
                return Aria2Token;
            }
        }


        public string Aria2RpcHostDisplay
        {
            get
            {
                if (Aria2UseLocal)
                {
                    return "本地Aria2";
                }
                return Aria2RpcHost;
            }
        }

        public string Aria2RpcHost
        {
            get
            {
                var rpc = Aria2UseLocal ? Aria2RpcLocal : Aria2Rpc;
                if (!string.IsNullOrWhiteSpace(rpc) && Uri.TryCreate(rpc, new UriCreationOptions(), out Uri? result))
                {
                    var port = result.Port > 0 ? (":" + result.Port.ToString()) : ("");
                    return $"{result.Host}{port}";
                }
                return "";
            }
        }

        public string Aria2LocalSavePath{ get; set; } = string.Empty;
        

        /// <summary>
        /// 默认使用本地的Aria2下载
        /// </summary>
        public bool Aria2UseLocal { get; set; } = true;



    }

    /// <summary>
    /// 配置项读取、写入、存储逻辑
    /// </summary>
    public partial class AppConfig
    {
        private static readonly AppConfig instance = new AppConfig();

        public static AppConfig Instance => instance;

        public AppConfigData ConfigData { set; get; } = new AppConfigData();

        private string _configPath = Path.Combine(Directory.GetCurrentDirectory(), @"Config.json");

        private object _lock = new object();

        


        private AppConfig()
        {
            Init();
            Debug.WriteLine(_configPath);
        }

        public void InitDefault() //载入默认配置
        {
            ConfigData.ClientId = Guid.NewGuid().ToString();
        }


        public bool Init()
        {
            try
            {

                Debug.WriteLine($"初始化配置" + Thread.CurrentThread.ManagedThreadId);
                if (File.Exists(_configPath) == false)
                {
                    Debug.WriteLine($"默认初始化");
                    InitDefault();
                    Save();
                }



                lock (_lock)
                {
                    var fileContent = File.ReadAllText(_configPath);
                    var appData = JsonConvert.DeserializeObject<AppConfigData>(fileContent);
                    ConfigData = appData;
                    ConfigData.PropertyChanged += AppConfigData_PropertyChanged;
                }

                if (string.IsNullOrWhiteSpace(ConfigData.ClientId))
                {
                    ConfigData.ClientId = Guid.NewGuid().ToString();
                    Save();
                }
                Debug.WriteLine($"初始化配置完毕");
                return true;
            }
            catch (Exception ex)
            {
                InitDefault();
                Save();
                Debug.WriteLine(ex);
                return false;
            }
            finally
            {
                if (ConfigData.AddSubscriptionFilterList.Count == 0)
                {
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简体"});
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简中" });
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简繁" });
                    ConfigData.AddSubscriptionFilterList.Add(new SubscriptionFilterModel() { Filter = "简日" });
                }

                ConfigData.Init();
            }
        }

        private void AppConfigData_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        

        public void Save()
        {
            try
            {
                lock (_lock)
                {
                    var data = JsonConvert.SerializeObject(ConfigData);
                    Debug.WriteLine($"存储配置{Thread.CurrentThread.ManagedThreadId} {data}");
                    File.WriteAllText(_configPath, data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }


    }
}
