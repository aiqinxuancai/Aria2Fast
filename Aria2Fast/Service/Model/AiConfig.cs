using Aria2Fast.Service.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged;
using System;

namespace Aria2Fast.Service
{
    /// <summary>
    /// 单组 AI 配置
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class AiConfig : BaseNotificationModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 配置名称（用于在列表中区分多组配置）
        /// </summary>
        public string Name { get; set; } = "新配置";

        [JsonConverter(typeof(StringEnumConverter))]
        public AiProtocolType Protocol { get; set; } = AiProtocolType.OpenAIChatCompletions;

        /// <summary>
        /// API 基础地址，例如 https://api.openai.com
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 协议名称（界面展示）
        /// </summary>
        [JsonIgnore]
        [DependsOn(nameof(Protocol))]
        public string ProtocolDisplayName => AiProtocol.GetDisplayName(Protocol);

        /// <summary>
        /// 当前 baseUrl + 协议 + 模型组合后的真实请求地址（界面展示）
        /// </summary>
        [JsonIgnore]
        [DependsOn(nameof(Protocol), nameof(BaseUrl), nameof(ModelName))]
        public string RequestUrlPreview
        {
            get
            {
                var url = AiProtocol.BuildRequestUrl(Protocol, BaseUrl, ModelName);
                return string.IsNullOrEmpty(url) ? "（请填写 API 地址）" : url;
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BaseUrl)
                && !string.IsNullOrWhiteSpace(ModelName)
                && !string.IsNullOrWhiteSpace(ApiKey);
        }

        public AiConfig Clone()
        {
            return new AiConfig
            {
                Id = Id,
                Name = Name,
                Protocol = Protocol,
                BaseUrl = BaseUrl,
                ModelName = ModelName,
                ApiKey = ApiKey
            };
        }

        public void CopyFrom(AiConfig other)
        {
            if (other == null)
            {
                return;
            }

            Name = other.Name;
            Protocol = other.Protocol;
            BaseUrl = other.BaseUrl;
            ModelName = other.ModelName;
            ApiKey = other.ApiKey;
        }
    }
}
