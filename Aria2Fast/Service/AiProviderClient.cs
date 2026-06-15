using Flurl.Http;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ClientModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal static class AiProviderClient
    {
        private const int DefaultTimeoutSeconds = 120;

        private static AiConfig? CurrentConfig => AppConfig.Instance.ConfigData.CurrentAiConfig;

        public static bool HasApiKey()
        {
            var config = CurrentConfig;
            return config != null && config.IsValid();
        }

        public static bool SupportsFunctionTools()
        {
            var config = CurrentConfig;
            if (config == null || !config.IsValid())
            {
                return false;
            }

            // 仅 OpenAI Chat Completions 协议走 SDK，支持函数调用
            return config.Protocol == AiProtocolType.OpenAIChatCompletions;
        }

        public static async Task<string> SendAsync(string userMessage, string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return string.Empty;
            }

            var config = CurrentConfig;
            if (config == null || !config.IsValid())
            {
                return string.Empty;
            }

            switch (config.Protocol)
            {
                case AiProtocolType.OpenAIChatCompletions:
                    return await SendOpenAIChatAsync(config, userMessage, systemMessage);
                case AiProtocolType.OpenAIResponses:
                    return await SendOpenAIResponsesAsync(config, userMessage, systemMessage);
                case AiProtocolType.Claude:
                    return await SendClaudeAsync(config, userMessage, systemMessage);
                case AiProtocolType.Gemini:
                    return await SendGeminiAsync(config, userMessage, systemMessage);
                default:
                    return string.Empty;
            }
        }

        public static async Task<string> SendWithToolsAsync(string userMessage, string systemMessage, IEnumerable<AiFunctionTool> tools)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || tools == null)
            {
                return string.Empty;
            }

            var config = CurrentConfig;
            if (config == null || !config.IsValid())
            {
                return string.Empty;
            }

            if (config.Protocol != AiProtocolType.OpenAIChatCompletions)
            {
                // 其他协议暂不支持函数调用，回退到普通对话
                return await SendAsync(userMessage, systemMessage);
            }

            var toolList = tools.Where(tool => tool != null).ToList();
            if (toolList.Count == 0)
            {
                return await SendAsync(userMessage, systemMessage);
            }

            return await SendOpenAIChatWithToolsAsync(config, userMessage, systemMessage, toolList);
        }

        #region OpenAI Chat Completions (SDK)

        private static ChatClient CreateOpenAIChatClient(AiConfig config)
        {
            var endpoint = new Uri(AiProtocol.NormalizeBaseUrl(config.BaseUrl) + ResolveOpenAiVersionPath(config.BaseUrl));
            var options = new OpenAIClientOptions
            {
                Endpoint = endpoint
            };

            return new ChatClient(model: config.ModelName, credential: new ApiKeyCredential(config.ApiKey), options: options);
        }

        /// <summary>
        /// OpenAI SDK 的 Endpoint 需要指向版本根（如 /v1），SDK 内部会拼 /chat/completions
        /// </summary>
        private static string ResolveOpenAiVersionPath(string baseUrl)
        {
            var root = AiProtocol.NormalizeBaseUrl(baseUrl);
            var lastSlash = root.LastIndexOf('/');
            var lastSegment = lastSlash >= 0 && lastSlash < root.Length - 1
                ? root.Substring(lastSlash + 1)
                : string.Empty;

            if (!string.IsNullOrEmpty(lastSegment)
                && lastSegment.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                && lastSegment.Length > 1
                && char.IsDigit(lastSegment[1]))
            {
                return string.Empty;
            }

            return "/v1";
        }

        private static async Task<string> SendOpenAIChatAsync(AiConfig config, string userMessage, string systemMessage)
        {
            try
            {
                var client = CreateOpenAIChatClient(config);
                var messages = CreateInitialMessages(userMessage, systemMessage);
                var completionResult = await client.CompleteChatAsync(messages);
                return completionResult.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AI] OpenAI Chat 请求失败：{ex.Message}");
                return string.Empty;
            }
        }

        private static async Task<string> SendOpenAIChatWithToolsAsync(AiConfig config, string userMessage, string systemMessage, IReadOnlyList<AiFunctionTool> tools)
        {
            try
            {
                var client = CreateOpenAIChatClient(config);
                var messages = CreateInitialMessages(userMessage, systemMessage);
                var toolMap = tools.ToDictionary(item => item.Name, StringComparer.Ordinal);
                var options = new ChatCompletionOptions
                {
                    AllowParallelToolCalls = false
                };

                foreach (var tool in tools)
                {
                    options.Tools.Add(ChatTool.CreateFunctionTool(
                        functionName: tool.Name,
                        functionDescription: tool.Description,
                        functionParameters: BinaryData.FromString(tool.JsonSchema)));
                }

                for (int round = 0; round < 6; round++)
                {
                    var completion = (await client.CompleteChatAsync(messages, options)).Value;

                    if (completion.ToolCalls.Count > 0)
                    {
                        messages.Add(new AssistantChatMessage(completion));

                        foreach (var toolCall in completion.ToolCalls)
                        {
                            if (!toolMap.TryGetValue(toolCall.FunctionName, out var tool))
                            {
                                messages.Add(new ToolChatMessage(toolCall.Id, "{\"error\":\"unknown tool\"}"));
                                continue;
                            }

                            string toolResult;
                            try
                            {
                                toolResult = await tool.InvokeAsync(toolCall.FunctionArguments.ToString());
                            }
                            catch (Exception ex)
                            {
                                EasyLogManager.Logger.Warning($"[AI] 工具执行失败：{toolCall.FunctionName} {ex.Message}");
                                toolResult = $"{{\"error\":\"{EscapeToolError(ex.Message)}\"}}";
                            }

                            messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                        }

                        continue;
                    }

                    return completion.Content.FirstOrDefault()?.Text ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AI] OpenAI Chat(工具) 请求失败：{ex.Message}");
                return string.Empty;
            }
        }

        private static List<ChatMessage> CreateInitialMessages(string userMessage, string systemMessage)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(systemMessage))
            {
                messages.Add(new SystemChatMessage(systemMessage));
            }

            messages.Add(new UserChatMessage(userMessage));
            return messages;
        }

        #endregion

        #region OpenAI Responses (HTTP)

        private static async Task<string> SendOpenAIResponsesAsync(AiConfig config, string userMessage, string systemMessage)
        {
            try
            {
                var url = AiProtocol.BuildRequestUrl(config.Protocol, config.BaseUrl, config.ModelName);
                var input = new List<object>();
                if (!string.IsNullOrWhiteSpace(systemMessage))
                {
                    input.Add(new { role = "system", content = systemMessage });
                }
                input.Add(new { role = "user", content = userMessage });

                var response = await url
                    .WithTimeout(TimeSpan.FromSeconds(DefaultTimeoutSeconds))
                    .WithHeader("Authorization", $"Bearer {config.ApiKey}")
                    .PostJsonAsync(new
                    {
                        model = config.ModelName,
                        input
                    })
                    .ReceiveString();

                var root = JObject.Parse(response);

                // 优先解析 output_text 便捷字段
                var outputText = root.Value<string>("output_text");
                if (!string.IsNullOrWhiteSpace(outputText))
                {
                    return outputText;
                }

                // 解析 output 数组中的文本
                var builder = new StringBuilder();
                if (root["output"] is JArray outputArray)
                {
                    foreach (var item in outputArray.OfType<JObject>())
                    {
                        if (item["content"] is JArray contentArray)
                        {
                            foreach (var contentItem in contentArray.OfType<JObject>())
                            {
                                var text = contentItem.Value<string>("text");
                                if (!string.IsNullOrEmpty(text))
                                {
                                    builder.Append(text);
                                }
                            }
                        }
                    }
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AI] OpenAI Responses 请求失败：{ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region Claude (HTTP)

        private static async Task<string> SendClaudeAsync(AiConfig config, string userMessage, string systemMessage)
        {
            try
            {
                var url = AiProtocol.BuildRequestUrl(config.Protocol, config.BaseUrl, config.ModelName);
                object body = string.IsNullOrWhiteSpace(systemMessage)
                    ? new
                    {
                        model = config.ModelName,
                        max_tokens = 4096,
                        messages = new[] { new { role = "user", content = userMessage } }
                    }
                    : new
                    {
                        model = config.ModelName,
                        max_tokens = 4096,
                        system = systemMessage,
                        messages = new[] { new { role = "user", content = userMessage } }
                    };

                var response = await url
                    .WithTimeout(TimeSpan.FromSeconds(DefaultTimeoutSeconds))
                    .WithHeader("x-api-key", config.ApiKey)
                    .WithHeader("anthropic-version", "2023-06-01")
                    .PostJsonAsync(body)
                    .ReceiveString();

                var root = JObject.Parse(response);
                var builder = new StringBuilder();
                if (root["content"] is JArray contentArray)
                {
                    foreach (var item in contentArray.OfType<JObject>())
                    {
                        if (string.Equals(item.Value<string>("type"), "text", StringComparison.OrdinalIgnoreCase))
                        {
                            builder.Append(item.Value<string>("text"));
                        }
                    }
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AI] Claude 请求失败：{ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region Gemini (HTTP)

        private static async Task<string> SendGeminiAsync(AiConfig config, string userMessage, string systemMessage)
        {
            try
            {
                var url = AiProtocol.BuildRequestUrl(config.Protocol, config.BaseUrl, config.ModelName);
                object body = string.IsNullOrWhiteSpace(systemMessage)
                    ? new
                    {
                        contents = new[]
                        {
                            new { role = "user", parts = new[] { new { text = userMessage } } }
                        }
                    }
                    : new
                    {
                        system_instruction = new { parts = new[] { new { text = systemMessage } } },
                        contents = new[]
                        {
                            new { role = "user", parts = new[] { new { text = userMessage } } }
                        }
                    };

                var response = await url
                    .WithTimeout(TimeSpan.FromSeconds(DefaultTimeoutSeconds))
                    .WithHeader("x-goog-api-key", config.ApiKey)
                    .PostJsonAsync(body)
                    .ReceiveString();

                var root = JObject.Parse(response);
                var text = root["candidates"]?
                    .FirstOrDefault()?["content"]?["parts"]?
                    .FirstOrDefault()?["text"]?.ToString();

                return text ?? string.Empty;
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[AI] Gemini 请求失败：{ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        private static string EscapeToolError(string message)
        {
            return (message ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    internal sealed class AiFunctionTool
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string JsonSchema { get; init; }
        public required Func<string, Task<string>> InvokeAsync { get; init; }
    }
}
