using ChatGPTSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal class RenameModel
    {
        public string New { get; set; }
        public string Old { get; set; }
    }


    internal class AutoRenameManager
    {

        const string kSystemMessage = """
                # 角色
                你是一个电影剧集格式化机器人，你非常擅长将输入的电影名称按照特定的格式规范化。

                ## 技能
                **技能1: 格式化电影或剧集文件名**
                - 确认输入的电影文件名，每行一个。
                - 擅长删除文件名中的无关内容，并保持主要的名称不变。
                - 按照规定的格式整理电影文件名，格式为 '剧集名称 SXXEXX.mkv'，其中SXX表示剧集季度，两位数字，EXX表示集数，也是两位数字，
                - 如果没有剧集的季，则季的部分应该保持S00。
                - 如果剧集的季和集都没有，则格式为'剧集名称.mkv'
                - 输出为Json数组格式：
                ```
                [{"old":"输入的剧集名称","new":"格式化后的剧集名称"}]
                ```

                ## 示例
                输入：
                [ANi] 歡迎來到實力至上主義的教室 第三季 - 08 [1080P][Baha][WEB-DL][AAC AVC][CHT].mp4 
                结果：
                [{"old":"[ANi] 歡迎來到實力至上主義的教室 第三季 - 08 [1080P][Baha][WEB-DL][AAC AVC][CHT].mp4 ","new":"歡迎來到實力至上主義的教室 S03E08.mp4 "}]

                ## 约束
                - 不要对剧集名称进行翻译或其他任何更改，不要对剧集名添加无用的空格。
                - 只使用原始提示使用的语言。
                - 您的回答应直接以优化的提示开始。
                - 不对返回内容进行解释。
                - 不输出json以外的其他内容。
                """;

        //static Dictionary<string, string> _cache = new Dictionary<string, string>();

        public static async Task<List<RenameModel>> GetNewNames(List<string> fileNames)
        {
            var client = new ChatGPTClient(AppConfig.Instance.ConfigData.OpenAIKey, timeoutSeconds: 60, proxyUri: AppConfig.Instance.ConfigData.OpenAIProxy);

            if (!string.IsNullOrEmpty(AppConfig.Instance.ConfigData.OpenAIHost))
            {
                client.Settings.APIURL = AppConfig.Instance.ConfigData.OpenAIHost;
            }

            if (client != null)
            {
                try
                {
                    var msg = string.Join("\n", fileNames);
                    var result = await client.SendMessage(msg, systemPrompt: kSystemMessage);

                    if (!string.IsNullOrEmpty(result.Response))
                    {
                        JObject root = JObject.Parse(result.Response);
                        List<RenameModel> objs = root.ToObject<List<RenameModel>>();
                        return objs;
                    }

                    return new List<RenameModel>();
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex.ToString());
                }
            }
            return new List<RenameModel>();
        }

    }
}
