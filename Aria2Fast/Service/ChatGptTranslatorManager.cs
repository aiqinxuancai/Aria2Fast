
using ChatGPTSharp;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Aria2Fast.Utils;

namespace Aria2Fast.Service
{
    internal class ChatGPTTranslatorManager
    {
        const string kSystemMessage = """
            # 角色
            你是一个精通提取文本的AI工程师，可以从复杂的文本中找到关键信息并返回。

            ## 技能
            - 识别用户提供的原始文本中的内容。
            - 根据文本内容，提取并返回作品的名称。

            ## 输出格式：
            直接返回一个JSON格式的字符串，字段为"title"。返回的JSON示例如下：
            ```
             { "title": "<被提取的作品名称>" }
            ```

            ## 限制：
            - 不增加额外的解释。
            - 当文本中含有多种语言的作品名称时，通常以"/"符号进行分割，仅返回第一种语言的名称。
            - 避免将字幕组名称及字幕名称等误识别为标题。
            - 不对作品名称进行编辑、翻译或字符转换。
            - 请移除作品名中的[]、【】等符号。
            """;

        static Dictionary<string, string> _cache = new Dictionary<string, string>();

        /// <summary>
        /// 完整调用一次提取episode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static async Task<string> GetEpisode(string s)
        {
            var client = new ChatGPTClient(AppConfig.Instance.ConfigData.OpenAIKey, "gpt-4o-mini", timeoutSeconds: 60, proxyUri: AppConfig.Instance.ConfigData.OpenAIProxy);

            if (!string.IsNullOrEmpty(AppConfig.Instance.ConfigData.OpenAIHost))
            {
                client.Settings.APIURL = AppConfig.Instance.ConfigData.OpenAIHost;
            }

            if (client != null)
            {
                try
                {
                    s = $"{s}";

                    if (_cache.TryGetValue(s, out var r))
                    {
                        return r;
                    }

                    var result = await client.SendMessage(s, systemPrompt: kSystemMessage);

                    if (!string.IsNullOrEmpty(result.Response))
                    {
                        JObject root = JObject.Parse(result.Response);
                        var title = (string)root["title"];
                        _cache[s] = title;
                        return title;
                    }

                    return string.Empty;
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex.ToString());
                }
            }
            return string.Empty;
        }

    }

}
