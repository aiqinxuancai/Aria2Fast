﻿// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var subscriptionModel = SubscriptionModel.FromJson(jsonString);

namespace WkyFast.Service.Model.SubscriptionModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using WkyApiSharp.Service.Model;

    public partial class SubscriptionModel : BaseNotificationModel
    {
        /// <summary>
        /// 订阅地址
        /// </summary>
        [JsonProperty("Url")]
        public string Url { get; set; }


        [JsonProperty("Device")]
        public WkyDevice Device { get; set; }

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


        /// <summary>
        /// 匹配任务总数
        /// </summary>
        [JsonIgnore]
        public string LastSubscriptionContent { 
            get 
            {
                if (AlreadyAddedDownloadModel.Count == 0)
                {
                    return "无";
                }
                else
                {
                    SubscriptionSubTaskModel last = AlreadyAddedDownloadModel.LastOrDefault();

                    if (last.Time != DateTime.MinValue) 
                    {
                        return $"{last.Name}\n{last.Time.ToString("yyyy-MM-dd HH:mm:ss")}";
                    }
                    else
                    {
                        return $"{last.Name}";//未赋值时间

                    }
                    
                }
            } 
        }


        /// <summary>
        /// 已经添加了下载的任务
        /// </summary>
        [JsonProperty("AlreadyAddedDownloadModel")]
        public ObservableCollection<SubscriptionSubTaskModel> AlreadyAddedDownloadModel { get; set; } = new ObservableCollection<SubscriptionSubTaskModel> { };
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
        public static SubscriptionModel[] FromJson(string json) => JsonConvert.DeserializeObject<SubscriptionModel[]>(json, WkyFast.Service.Model.SubscriptionModel.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this SubscriptionModel[] self) => JsonConvert.SerializeObject(self, WkyFast.Service.Model.SubscriptionModel.Converter.Settings);
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
}
