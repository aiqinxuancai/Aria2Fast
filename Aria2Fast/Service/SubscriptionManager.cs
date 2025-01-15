using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aria2Fast.Service.Model;
using Newtonsoft.Json;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Xml.Linq;
using System.Diagnostics;
using Flurl.Http;
using System.Threading;
using System.Collections.ObjectModel;
using Aria2Fast.Utils;
using Aria2Fast.Service.Model.SubscriptionModel;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net;
using MemoryPack;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Aria2Fast.Service
{
    public class SubscriptionManager
    {
        private const int kTimeOutSec = 20;

        /// <summary>
        /// 订阅进度变化 当前，总数，当前名称
        /// </summary>
        public event Action<int, int, string> OnSubscriptionProgressChanged;

        private static SubscriptionManager instance = new SubscriptionManager();

        public static SubscriptionManager Instance
        {
            get
            {
                return instance;
            }
        }

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ObservableCollection<SubscriptionModel> SubscriptionModel { get; set; } = new ObservableCollection<SubscriptionModel>();

        /// <summary>
        /// 由每次加载SubscriptionModel时，进行初始化，本身不存储
        /// TODO 存储所有的订阅下载 使用MemoryPack
        /// </summary>
        public Dictionary<string, string> TaskUrlToSubscriptionName { get; set; } = new ();

        public bool Subscribing { get; set; } = false;

        private object _look = new object();
        private object _lookForSave = new object();

        public SubscriptionManager()
        {
            SubscriptionModel = new ObservableCollection<SubscriptionModel>();
            //SubscriptionModel.CollectionChanged += SubscriptionModel_CollectionChanged;

            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginResultEvent>()
                .SubscribeOnMainThread(async r =>
                {
                    Restart();

                });
        }

        ~SubscriptionManager()
        {
            _tokenSource.Cancel();
        }

        //private void SubscriptionModel_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    Save();
        //}

        /// <summary>
        /// 载入并启动订阅刷新
        /// </summary>
        public void Restart()
        {
            Load();
            Start();
        }

        public void Start()
        {
            if (_tokenSource != null) //TODO 停止任务
            {
                _tokenSource.Cancel();
            }

            _tokenSource = new CancellationTokenSource();

            Task longRunningTask = Task.Factory.StartNew((object? token) =>
            {
                TimerFunc(_tokenSource.Token);
            }, _tokenSource.Token, TaskCreationOptions.LongRunning);

        }

        public void Stop()
        {
            if (_tokenSource != null) //TODO 停止任务
            {
                _tokenSource.Cancel();
            }
        }

        /// <summary>
        /// 10分钟检查一次订阅
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void TimerFunc(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                try
                {
                    CheckSubscription(Aria2ApiManager.Instance.CurrentRpc);
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex.ToString());
                }
                

                TaskHelper.Sleep(1000 * 60 * 10, 100, cancellationToken);
            }
        }


        public static bool CheckTitle(string matchStr, bool matchIsRegex, string title) 
        {
            //检查是否符合过滤方法

            try
            {
                if (!string.IsNullOrWhiteSpace(matchStr))
                {
                    if (matchIsRegex)
                    {
                        Regex regex = new Regex(matchStr);
                        if (!regex.IsMatch(title))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        //检查是否包含文本
                        var strList = matchStr.Split("|");
                        var count = 0;
                        foreach (var str in strList)
                        {
                            if (title.Contains(str))
                            {
                                count++;
                            }
                        }
                        if (count == 0)
                        {
                            return false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
                return true;
            }


            return true;
        }


        /// <summary>
        /// 是否可以下载
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private bool CheckTitle(SubscriptionModel subscription, string title)
        {
            //检查是否符合过滤方法

            try
            {
                if (!string.IsNullOrWhiteSpace(subscription.Filter))
                {
                    if (subscription.IsFilterRegex)
                    {
                        Regex regex = new Regex(subscription.Filter);
                        if (!regex.IsMatch(title))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        //检查是否包含文本
                        var strList = subscription.Filter.Split("|");
                        var count = 0;
                        foreach (var str in strList)
                        {
                            if (title.Contains(str))
                            {
                                count++;
                            }
                        }
                        if (count == 0)
                        {
                            return false;
                        }
                    }
                }

            } 
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex.ToString());
                return true;
            }

            
            return true;
        }

        private int GetMatchTaskCount(IEnumerable<SyndicationItem> Items, SubscriptionModel model)
        {
            int count = 0;
            foreach (SyndicationItem item in Items)
            {
                string subject = item.Title.Text;
                string summary = item.Summary?.Text;
                if (CheckTitle(model, subject))
                {
                    count++;
                }

            }
            return count;

        }

        /// <summary>
        /// 通过网络获取订阅地址的Model
        /// </summary>
        /// <param name="url"></param>
        public SubscriptionInfoModel GetSubscriptionInfo(string url)
        {
            try
            {
                var feed = LoadSyndicationFeedAsync(url).Result;
                if (feed == null)
                {
                    return null;
                }

                SubscriptionInfoModel model = new SubscriptionInfoModel();
                foreach (SyndicationItem item in feed.Items)
                {
                    string subject = item.Title.Text;
                    model.SubRssTitles.Add(subject);
                }
                model.SubscriptionName = feed.Title.Text;

                return model;
            }
            catch (Exception e)
            {
                EasyLogManager.Logger.Error($"获取订阅标题失败 {url} {e}");
                return null;
            }
        }

        /// <summary>
        /// 检查一次订阅
        /// </summary>
        private async void CheckSubscription(string currentRpc)
        {
            if (Subscribing)
            {
                EasyLogManager.Logger.Info("当前正在检查订阅，不再执行");
                return;
            }
            Subscribing = true;
            EasyLogManager.Logger.Info("开始检查订阅");

            var copyList = new List<SubscriptionModel>(SubscriptionModel);
            OnSubscriptionProgressChanged?.Invoke(0, copyList.Count, string.Empty);

            for (int i = 0; i < copyList.Count; i++)
            {
                OnSubscriptionProgressChanged?.Invoke(i, copyList.Count, copyList[i].Name);
                await CheckSubscriptionOne(copyList[i], currentRpc);
            }

            OnSubscriptionProgressChanged?.Invoke(copyList.Count, copyList.Count, string.Empty);
            EasyLogManager.Logger.Info("订阅检查完毕");
            Subscribing = false;
        }

        public async Task CheckSubscriptionOne(SubscriptionModel subscription, string currentRpc)
        {
            string url = subscription.Url;

            if (subscription.AlreadyAddedDownloadModel == null)
            {
                subscription.AlreadyAddedDownloadModel = new ObservableCollection<SubscriptionSubTaskModel> { };
            }

            var feed = await LoadSyndicationFeedAsync(url);
            if (feed == null)
            {
                return;
            }

            subscription.TaskFullCount = feed.Items.Count();
            subscription.Name = feed.Title.Text;
            subscription.TaskMatchCount = GetMatchTaskCount(feed.Items, subscription);

            foreach (SyndicationItem item in feed.Items)
            {
                string subject = item.Title.Text;
                string summary = item.Summary?.Text;

                if (!CheckTitle(subscription, subject))
                {
                    continue;
                }

                var savePath = subscription.SavePath;

                foreach (var link in item.Links)
                {
                    string downloadUrl = link.Uri.ToString();
                    if (!subscription.AlreadyAddedDownloadModel.Any(a => a.Name == subject))
                    {
                        if (link.RelationshipType == "enclosure" ||
                           (!string.IsNullOrWhiteSpace(link.MediaType) && link.MediaType.Contains("bittorrent")))
                        {
                            try
                            {
                                savePath = await AutoEpisodeTitle(subscription, subject, savePath);

                                if (currentRpc != Aria2ApiManager.Instance.CurrentRpc)
                                {
                                    return;
                                }

                                EasyLogManager.Logger.Info($"添加下载{subject} {link.Uri} {savePath}");
                                var aria2Result = await Aria2ApiManager.Instance.DownloadBtFileUrl(downloadUrl, savePath);

                                EasyLogManager.Logger.Info($"添加下载完毕");

                                if (aria2Result.IsSuccessed)
                                {
                                    TaskUrlToSubscriptionName[aria2Result.Gid] = subject;
                                    if (!string.IsNullOrWhiteSpace(aria2Result.InfoHash))
                                    {
                                        TaskUrlToSubscriptionName[aria2Result.InfoHash] = subject;
                                    }
                                }

                                if (aria2Result.IsSuccessed)
                                {
                                    subscription.AlreadyAddedDownloadModel.Add(new SubscriptionSubTaskModel() { Name = subject, Url = downloadUrl, Time = DateTime.Now });
                                    EasyLogManager.Logger.Info($"添加成功");
                                }
                                else
                                {
                                    EasyLogManager.Logger.Error($"添加失败");
                                }
                            }
                            catch (Exception ex)
                            {
                                EasyLogManager.Logger.Error(ex.ToString());
                            }
                        }
                    }
                }
            }
            Save();
        }

        private static async Task<string> AutoEpisodeTitle(SubscriptionModel subscription, string subject, string savePath)
        {
            var episodeTitle = "";
            //使用自动的剧集名称，来源gpt
            if (subscription.AutoDir)
            {
                EasyLogManager.Logger.Info($"使用自动目录分组");
                if (subscription.EpisodeTitleList == null)
                {
                    subscription.EpisodeTitleList = new List<string>();
                }
                episodeTitle = subscription.EpisodeTitleList.FirstOrDefault(a => subject.Contains(a));
                if (string.IsNullOrEmpty(episodeTitle) && AppConfig.Instance.ConfigData.OpenAIOpen)
                {
                    EasyLogManager.Logger.Info($"获取剧集名称...");
                    //使用ChatGPT
                    episodeTitle = await ChatGPTTranslatorManager.GetEpisode(subject);

                    EasyLogManager.Logger.Info($"剧集名称：{episodeTitle}");
                    if (!string.IsNullOrEmpty(episodeTitle))
                    {
                        subscription.EpisodeTitleList.Add(episodeTitle);
                    }
                }
            }

            if (!string.IsNullOrEmpty(episodeTitle))
            {
                savePath = savePath + "/" + PathHelper.RemoveInvalidChars(episodeTitle);
            }

            return savePath;
        }

        public void LoadTrueName()
        {

            try
            {
                string fileName = @$"UrlNameTable.bin";
                TaskUrlToSubscriptionName = new Dictionary<string, string>();

                if (File.Exists(fileName))
                {
                    var fileContent = File.ReadAllBytes(fileName);
                    var val = MemoryPackSerializer.Deserialize<Dictionary<string, string>>(fileContent);

                    if (val != null)
                    {
                        TaskUrlToSubscriptionName = val;
                    }

                }
            } 
            catch (Exception ex)
            {
                EasyLogManager.Logger.Info(ex);
            }

        }


        public void SaveTrueName()
        {
            string fileName = @$"UrlNameTable.bin";

            var bin = MemoryPackSerializer.Serialize(TaskUrlToSubscriptionName);

            try
            {
                File.WriteAllBytes(fileName, bin);
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }




        }

        public void Load()
        {
            lock (_lookForSave)
            {

                LoadTrueName();

                var rpc = Aria2ApiManager.Instance.CurrentRpc;

                if (string.IsNullOrWhiteSpace(rpc))
                {
                    SubscriptionModel.Clear();
                    return;
                }

                var uri = new Uri(rpc);

                string fileName = @$"Subscription_{uri.Host}.json";
                Debug.WriteLine($"准备载入{fileName}");

                //名称URL
                //载入TaskUrlToSubscriptionName
                SubscriptionModel.Clear();

                if (File.Exists(fileName))
                {
                    List<SubscriptionModel> subscriptionModel = JsonConvert.DeserializeObject<List<SubscriptionModel>>(File.ReadAllText(fileName));

                    if (subscriptionModel != null)
                    {
                        foreach (SubscriptionModel item in subscriptionModel)
                        {
                            SubscriptionModel.Add(item);

                            //加载名称表
                            foreach (var m in item.AlreadyAddedDownloadModel)
                            {
                                var taskUrl = m.Url;
                                if (!string.IsNullOrWhiteSpace(taskUrl))
                                {
                                    TaskUrlToSubscriptionName[taskUrl] = m.Name;
                                }
                                
                            }
                        }
                    }

                }

                SaveTrueName();




            }
        }


        public void Save()
        {
            lock (_lookForSave)
            {
                Debug.WriteLine("保存订阅");

                try
                {
                    var rpc = Aria2ApiManager.Instance.CurrentRpc;
                    var uri = new Uri(rpc);
                    string fileName = @$"Subscription_{uri.Host}.json";
                    var content = JsonConvert.SerializeObject(SubscriptionModel);
                    File.WriteAllText(fileName, content);
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex);
                }
                try
                {
                    SaveTrueName();
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex);
                }
            }

        }

        //存储订阅，读取加载订阅

        public bool Add(string url, string path, int season = 0, string namePath = "", string keyword = "", bool keywordIsRegex = false, bool autoDir = false, SubscriptionModel? sourceModel = null)
        {

            if (sourceModel == null)
            {
                if (SubscriptionModel.ToList().Find(a => { return a.Url == url; }) != null)
                {
                    //找到了存在相同
                    EasyLogManager.Logger.Error($"添加失败，重复的订阅");
                    return false;
                }
            }
            else
            {
                SubscriptionModel.Remove(sourceModel);
            }

            SubscriptionModel model = new SubscriptionModel();
            if (sourceModel != null)
            {
                model = sourceModel;
            }

            model.Url = url;
            model.Filter = keyword;
            model.IsFilterRegex = keywordIsRegex;
            model.Path = path;
            model.AutoDir = autoDir;
            model.Season = season;
            model.Name = namePath;
            model.NamePath = namePath;
            //识别季？

            EasyLogManager.Logger.Error($"添加订阅：{model.Url}");

            SubscriptionModel.Add(model);

            Save();

            Task.Run(() => {
                CheckSubscription(Aria2ApiManager.Instance.CurrentRpc);
            });
            
            return true;
        }

        public void Remove(string url)
        {
            for (int i = 0; i < SubscriptionModel.Count; i++)
            {
                if (SubscriptionModel[i].Url == url)
                {
                    SubscriptionModel.RemoveAt(i);
                    break; //只删除一个
                }
            }
        }


        private async Task<SyndicationFeed> LoadSyndicationFeedAsync(string url)
        {
            try
            {
                XmlReader reader;
                SyndicationFeed feed;

                if (AppConfig.Instance.ConfigData.SubscriptionProxyOpen && !string.IsNullOrEmpty(AppConfig.Instance.ConfigData.SubscriptionProxy))
                {
                    var proxyUrl = AppConfig.Instance.ConfigData.SubscriptionProxy;
                    var proxy = new WebProxy(proxyUrl);
                    var handler = new HttpClientHandler() { Proxy = proxy };
                    var client = new HttpClient(handler);

                    client.Timeout = TimeSpan.FromSeconds(kTimeOutSec);
                    var response = await client.GetAsync(url);

                    reader = XmlReader.Create(await response.Content.ReadAsStreamAsync());
                    feed = SyndicationFeed.Load(reader);
                    reader.Close();
                }
                else
                {
                    var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(kTimeOutSec);
                    var response = await client.GetAsync(url);
                    reader = XmlReader.Create(await response.Content.ReadAsStreamAsync());
                    feed = SyndicationFeed.Load(reader);
                    reader.Close();
                }

                return feed;
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error($"无法访问订阅：{url} \n {ex}");
                return null;
            }
        }

    }
}
