using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Aria2Fast.Service.Model
{

    public record Aria2Event
    {
        protected Aria2Event()
        {
        }

        /// <summary>
        /// 事件类型
        /// </summary>
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public virtual Events Type { get; set; }


    }

    public record LoginResultEvent : Aria2Event
    {
        public bool IsSuccess { get; set; }

        public LoginResultEvent(bool isSuccess) => (IsSuccess) = (isSuccess);

        public override Events Type { get; set; } = Events.LoginResultEvent;
    }

    public record LoginStartEvent : Aria2Event
    {
        public override Events Type { get; set; } = Events.LoginStartEvent;
    }

    public record DownloadSuccessEvent : Aria2Event
    {
        public string Name { get; set; }

        public DownloadSuccessEvent(string name) => (Name) = (name);

        public override Events Type { get; set; } = Events.DownloadSuccessEvent;
    }

    public record MikanListLoaded : Aria2Event
    {
        public override Events Type { get; set; } = Events.MikanListLoaded;
    }

    public record MikanListProgressEvent : Aria2Event
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string Name { get; set; } = string.Empty;

        public override Events Type { get; set; } = Events.MikanListProgressEvent;
    }

    public record MikanAiProgressEvent : Aria2Event
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string Name { get; set; } = string.Empty;

        public override Events Type { get; set; } = Events.MikanAiProgressEvent;
    }

    public enum Events
    {
        /// <summary>
        /// 验证RPC成功
        /// </summary>
        [Description("LoginResultEvent")]
        [EnumMember(Value = "LoginResultEvent")]
        LoginResultEvent,


        /// <summary>
        /// 开始验证RPC
        /// </summary>
        [Description("LoginStartEvent")]
        [EnumMember(Value = "LoginStartEvent")]
        LoginStartEvent,


        /// <summary>
        /// 下载成功
        /// </summary>
        [Description("DownloadSuccessEvent")]
        [EnumMember(Value = "DownloadSuccessEvent")]
        DownloadSuccessEvent,



        /// <summary>
        /// Mikan加载完毕
        /// </summary>
        [Description("MikanListLoaded")]
        [EnumMember(Value = "MikanListLoaded")]
        MikanListLoaded,

        /// <summary>
        /// Mikan列表加载进度
        /// </summary>
        [Description("MikanListProgressEvent")]
        [EnumMember(Value = "MikanListProgressEvent")]
        MikanListProgressEvent,

        /// <summary>
        /// Mikan AI请求进度
        /// </summary>
        [Description("MikanAiProgressEvent")]
        [EnumMember(Value = "MikanAiProgressEvent")]
        MikanAiProgressEvent,
    }
}
