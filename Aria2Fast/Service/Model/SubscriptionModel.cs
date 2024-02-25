﻿// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var subscriptionModel = SubscriptionModel.FromJson(jsonString);

namespace Aria2Fast.Service.Model.SubscriptionModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using MemoryPack;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class SubscriptionModel : BaseNotificationModel
    {
        /// <summary>
        /// 订阅地址
        /// </summary>
        [JsonProperty("Url")]
        public string Url { get; set; }

        [JsonProperty("AutoDir")]
        public bool AutoDir { get; set; }


        [JsonProperty("EpisodeTitleList")]
        public List<string> EpisodeTitleList { get; set; }

        /// <summary>
        /// 存储路径
        /// </summary>
        [JsonProperty("Path")] 
        public string Path { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Filter")]
        public string Filter { get; set; }

        [JsonProperty("IsFilterRegex")]
        public bool IsFilterRegex { get; set; }

        /// <summary>
        /// 任务总数
        /// </summary>
        [JsonProperty("TaskFullCount")]
        public int TaskFullCount { get; set; }

        /// <summary>
        /// 匹配任务总数
        /// </summary>
        [JsonProperty("TaskMatchCount")]
        public int TaskMatchCount { get; set; }


        [JsonIgnore]
        private string _lastSubscriptionContent;

        /// <summary>
        /// 匹配任务总数
        /// </summary>
        [JsonIgnore]
        public string LastSubscriptionContent { 
            get 
            {
                return _lastSubscriptionContent;
            } 
        }


        /// <summary>
        /// 已经添加了下载的任务
        /// </summary>
        [JsonProperty("AlreadyAddedDownloadModel")]
        public ObservableCollection<SubscriptionSubTaskModel> AlreadyAddedDownloadModel { get; set; } = new ObservableCollection<SubscriptionSubTaskModel> { };


        public SubscriptionModel()
        {
            AlreadyAddedDownloadModel.CollectionChanged += AlreadyAddedDownloadModel_CollectionChanged;
        }

        private void AlreadyAddedDownloadModel_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            //变化，需要刷新 LastSubscriptionContent

            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (AlreadyAddedDownloadModel.Count == 0)
                {
                    _lastSubscriptionContent = "无";
                }
                else
                {
                    SubscriptionSubTaskModel last = AlreadyAddedDownloadModel.LastOrDefault();

                    if (last.Time != DateTime.MinValue)
                    {
                        _lastSubscriptionContent = $"{last.Name}\n{last.Time.ToString("yyyy-MM-dd HH:mm:ss")}";
                    }
                    else
                    {
                        _lastSubscriptionContent = $"{last.Name}";//未赋值时间

                    }
                    OnPropertyChanged("LastSubscriptionContent");
                }
            }

            
        }
    }

    public partial class SubscriptionSubTaskModel : BaseNotificationModel
    {
        [JsonProperty("Url")]
        public string Url { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Time")]
        public DateTime Time { get; set; }
        
    }

    public partial class SubscriptionModel
    {
        public static SubscriptionModel[] FromJson(string json) => JsonConvert.DeserializeObject<SubscriptionModel[]>(json, Aria2Fast.Service.Model.SubscriptionModel.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this SubscriptionModel[] self) => JsonConvert.SerializeObject(self, Aria2Fast.Service.Model.SubscriptionModel.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }


    /// <summary>
    /// Info
    /// </summary>
    public partial class SubscriptionInfoModel : BaseNotificationModel
    {
        public string SubscriptionName { get; set; }

        public List<string> SubRssTitles { get; set; } = new List<string>();
    }
}
