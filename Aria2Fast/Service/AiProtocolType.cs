using System;
using System.Collections.Generic;

namespace Aria2Fast.Service
{
    /// <summary>
    /// AI 接口协议类型
    /// </summary>
    public enum AiProtocolType
    {
        /// <summary>
        /// OpenAI Chat Completions (/v1/chat/completions)，兼容绝大多数中转/第三方服务，支持函数调用
        /// </summary>
        OpenAIChatCompletions,

        /// <summary>
        /// OpenAI Responses (/v1/responses)
        /// </summary>
        OpenAIResponses,

        /// <summary>
        /// Anthropic Claude (Messages API, /v1/messages)
        /// </summary>
        Claude,

        /// <summary>
        /// Google Gemini (generateContent)
        /// </summary>
        Gemini
    }

    /// <summary>
    /// 协议相关的元数据与 URL 组合逻辑
    /// </summary>
    public static class AiProtocol
    {
        public sealed class ProtocolOption
        {
            public AiProtocolType Type { get; init; }
            public string DisplayName { get; init; } = string.Empty;
            public string DefaultBaseUrl { get; init; } = string.Empty;
            public string DefaultModel { get; init; } = string.Empty;
        }

        /// <summary>
        /// 协议下拉框选项（顺序即展示顺序）
        /// </summary>
        public static IReadOnlyList<ProtocolOption> Options { get; } = new[]
        {
            new ProtocolOption
            {
                Type = AiProtocolType.OpenAIChatCompletions,
                DisplayName = "OpenAI - Chat Completions",
                DefaultBaseUrl = "https://api.openai.com",
                DefaultModel = "gpt-5.5"
            },
            new ProtocolOption
            {
                Type = AiProtocolType.OpenAIResponses,
                DisplayName = "OpenAI - Responses",
                DefaultBaseUrl = "https://api.openai.com",
                DefaultModel = "gpt-5.5"
            },
            new ProtocolOption
            {
                Type = AiProtocolType.Claude,
                DisplayName = "Claude (Anthropic)",
                DefaultBaseUrl = "https://api.anthropic.com",
                DefaultModel = "claude-sonnet-4-6"
            },
            new ProtocolOption
            {
                Type = AiProtocolType.Gemini,
                DisplayName = "Gemini (Google)",
                DefaultBaseUrl = "https://generativelanguage.googleapis.com",
                DefaultModel = "gemini-3-flash-preview"
            }
        };

        public static string GetDisplayName(AiProtocolType type)
        {
            foreach (var option in Options)
            {
                if (option.Type == type)
                {
                    return option.DisplayName;
                }
            }
            return type.ToString();
        }

        public static ProtocolOption GetOption(AiProtocolType type)
        {
            foreach (var option in Options)
            {
                if (option.Type == type)
                {
                    return option;
                }
            }
            return Options[0];
        }

        /// <summary>
        /// 预设平台配置（用于模态弹窗一键填入）
        /// </summary>
        public sealed class Preset
        {
            public string DisplayName { get; init; } = string.Empty;
            public AiProtocolType Protocol { get; init; }
            public string BaseUrl { get; init; } = string.Empty;
            public string ModelName { get; init; } = string.Empty;
        }

        /// <summary>
        /// 主流平台预设（含各家最新主流模型与官方地址，数据截至 2026-06）
        /// </summary>
        public static IReadOnlyList<Preset> Presets { get; } = new[]
        {
            new Preset
            {
                DisplayName = "DeepSeek - deepseek-v4-flash",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.deepseek.com",
                ModelName = "deepseek-v4-flash"
            },
            new Preset
            {
                DisplayName = "DeepSeek - deepseek-v4-pro",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.deepseek.com",
                ModelName = "deepseek-v4-pro"
            },
            new Preset
            {
                DisplayName = "通义千问 Qwen - qwen3-max",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ModelName = "qwen3-max"
            },
            new Preset
            {
                DisplayName = "通义千问 Qwen - qwen-plus",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ModelName = "qwen-plus"
            },
            new Preset
            {
                DisplayName = "智谱 GLM - glm-4.6",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
                ModelName = "glm-4.6"
            },
            new Preset
            {
                DisplayName = "智谱 GLM - glm-4.7-flash",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
                ModelName = "glm-4.7-flash"
            },
            new Preset
            {
                DisplayName = "MiniMax - MiniMax-M2.1",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.minimaxi.com/v1",
                ModelName = "MiniMax-M2.1"
            },
            new Preset
            {
                DisplayName = "Kimi (Moonshot) - kimi-k2.6",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.moonshot.cn/v1",
                ModelName = "kimi-k2.6"
            },
            new Preset
            {
                DisplayName = "火山方舟 Doubao - doubao-seed-1.6",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://ark.cn-beijing.volces.com/api/v3",
                ModelName = "doubao-seed-1-6-250615"
            },
            new Preset
            {
                DisplayName = "OpenAI - gpt-5.5",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.openai.com",
                ModelName = "gpt-5.5"
            },
            new Preset
            {
                DisplayName = "OpenAI - gpt-4o-mini",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.openai.com",
                ModelName = "gpt-4o-mini"
            },
            new Preset
            {
                DisplayName = "Claude - claude-opus-4-8",
                Protocol = AiProtocolType.Claude,
                BaseUrl = "https://api.anthropic.com",
                ModelName = "claude-opus-4-8"
            },
            new Preset
            {
                DisplayName = "Claude - claude-sonnet-4-6",
                Protocol = AiProtocolType.Claude,
                BaseUrl = "https://api.anthropic.com",
                ModelName = "claude-sonnet-4-6"
            },
            new Preset
            {
                DisplayName = "Gemini - gemini-3-flash-preview",
                Protocol = AiProtocolType.Gemini,
                BaseUrl = "https://generativelanguage.googleapis.com",
                ModelName = "gemini-3-flash-preview"
            },
            new Preset
            {
                DisplayName = "Gemini - gemini-3-pro-preview",
                Protocol = AiProtocolType.Gemini,
                BaseUrl = "https://generativelanguage.googleapis.com",
                ModelName = "gemini-3-pro-preview"
            },
            new Preset
            {
                DisplayName = "Gemini - gemini-2.5-flash（稳定）",
                Protocol = AiProtocolType.Gemini,
                BaseUrl = "https://generativelanguage.googleapis.com",
                ModelName = "gemini-2.5-flash"
            },
            new Preset
            {
                DisplayName = "OpenRouter - deepseek-v4",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://openrouter.ai/api/v1",
                ModelName = "deepseek/deepseek-v4"
            },
            new Preset
            {
                DisplayName = "硅基流动 SiliconFlow - DeepSeek-V3.2",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://api.siliconflow.cn/v1",
                ModelName = "deepseek-ai/DeepSeek-V3.2-Exp"
            },
            new Preset
            {
                DisplayName = "Aihubmix（中转）- gpt-5.5",
                Protocol = AiProtocolType.OpenAIChatCompletions,
                BaseUrl = "https://aihubmix.com",
                ModelName = "gpt-5.5"
            }
        };

        /// <summary>
        /// 去掉结尾的 "/"，并补全协议头
        /// </summary>
        public static string NormalizeBaseUrl(string baseUrl)
        {
            var url = (baseUrl ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            return url.TrimEnd('/');
        }

        /// <summary>
        /// 根据协议类型与 baseUrl、模型名，组合出真实的请求地址（用于界面预览与实际请求）
        /// </summary>
        public static string BuildRequestUrl(AiProtocolType type, string baseUrl, string model)
        {
            var root = NormalizeBaseUrl(baseUrl);
            if (string.IsNullOrEmpty(root))
            {
                return string.Empty;
            }

            switch (type)
            {
                case AiProtocolType.OpenAIChatCompletions:
                    return CombineOpenAiPath(root, "chat/completions");
                case AiProtocolType.OpenAIResponses:
                    return CombineOpenAiPath(root, "responses");
                case AiProtocolType.Claude:
                    return CombineClaudePath(root);
                case AiProtocolType.Gemini:
                    var modelName = string.IsNullOrWhiteSpace(model) ? "{model}" : model.Trim();
                    return CombineGeminiPath(root, modelName);
                default:
                    return root;
            }
        }

        /// <summary>
        /// OpenAI 协议：若 baseUrl 未包含版本段（/v1 等），自动补 /v1
        /// </summary>
        private static string CombineOpenAiPath(string root, string suffix)
        {
            if (EndsWithVersionSegment(root))
            {
                return $"{root}/{suffix}";
            }
            return $"{root}/v1/{suffix}";
        }

        private static string CombineClaudePath(string root)
        {
            if (EndsWithVersionSegment(root))
            {
                return $"{root}/messages";
            }
            return $"{root}/v1/messages";
        }

        private static string CombineGeminiPath(string root, string model)
        {
            // 形如 https://host/v1beta/models/{model}:generateContent
            if (root.EndsWith("/v1beta", StringComparison.OrdinalIgnoreCase)
                || root.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{root}/models/{model}:generateContent";
            }
            return $"{root}/v1beta/models/{model}:generateContent";
        }

        /// <summary>
        /// 判断 baseUrl 是否已经以版本段结尾（避免重复拼接 /v1）
        /// </summary>
        private static bool EndsWithVersionSegment(string root)
        {
            var lastSegment = root;
            var slashIndex = root.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < root.Length - 1)
            {
                lastSegment = root.Substring(slashIndex + 1);
            }

            if (string.IsNullOrEmpty(lastSegment))
            {
                return false;
            }

            // v1, v1beta, v2, beta 等
            if (lastSegment.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                && lastSegment.Length > 1
                && char.IsDigit(lastSegment[1]))
            {
                return true;
            }

            return lastSegment.Equals("beta", StringComparison.OrdinalIgnoreCase);
        }
    }
}
