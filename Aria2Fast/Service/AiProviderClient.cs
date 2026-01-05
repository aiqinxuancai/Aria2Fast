using Google.GenAI;
using Google.GenAI.Types;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ClientModel;

namespace Aria2Fast.Service
{
    internal static class AiProviderClient
    {
        private const string OpenAIDefaultModel = "gpt-4o-mini";
        private const string DeepSeekDefaultModel = "deepseek-chat";
        private const string MiniMaxDefaultModel = "MiniMax-M2.1";
        private const string GeminiDefaultModel = "gemini-2.5-flash";

        private static readonly Uri DeepSeekEndpoint = new Uri("https://api.deepseek.com/v1");
        private static readonly Uri MiniMaxEndpoint = new Uri("https://api.minimax.io/v1");
        private static readonly Uri AihubmixEndpoint = new Uri("https://aihubmix.com/v1");
        private static readonly Uri OpenRouterEndpoint = new Uri("https://openrouter.ai/api/v1");

        public static bool HasApiKey()
        {
            return !string.IsNullOrWhiteSpace(GetApiKey());
        }

        public static async Task<string> SendAsync(string userMessage, string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return string.Empty;
            }

            var provider = AppConfig.Instance.ConfigData.AiProvider;
            if (provider == AiProviderType.Gemini)
            {
                return await SendGeminiAsync(userMessage, systemMessage);
            }

            return await SendOpenAICompatibleAsync(provider, userMessage, systemMessage);
        }

        private static async Task<string> SendOpenAICompatibleAsync(AiProviderType provider, string userMessage, string systemMessage)
        {
            var apiKey = GetApiKey(provider);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return string.Empty;
            }

            var options = BuildClientOptions(provider);
            var model = GetModel(provider);
            if (string.IsNullOrWhiteSpace(model))
            {
                return string.Empty;
            }
            ChatClient client = options == null
                ? new ChatClient(model: model, apiKey: apiKey)
                : new ChatClient(model: model, credential: new ApiKeyCredential(apiKey), options: options);

            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(systemMessage))
            {
                messages.Add(new SystemChatMessage(systemMessage));
            }
            messages.Add(new UserChatMessage(userMessage));

            var completionResult = await client.CompleteChatAsync(messages);
            var content = completionResult.Value.Content.FirstOrDefault()?.Text;
            return content ?? string.Empty;
        }

        private static async Task<string> SendGeminiAsync(string userMessage, string systemMessage)
        {
            var apiKey = GetApiKey(AiProviderType.Gemini);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return string.Empty;
            }

            var client = new Client(apiKey: apiKey);

            GenerateContentConfig? config = null;
            if (!string.IsNullOrWhiteSpace(systemMessage))
            {
                config = new GenerateContentConfig
                {
                    SystemInstruction = new Content
                    {
                        Parts = new List<Part>
                        {
                            new Part { Text = systemMessage }
                        }
                    }
                };
            }

            var response = await client.Models.GenerateContentAsync(
                model: GeminiDefaultModel,
                contents: userMessage,
                config: config
            );

            var text = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            return text ?? string.Empty;
        }

        private static OpenAIClientOptions? BuildClientOptions(AiProviderType provider)
        {
            var endpoint = GetEndpoint(provider);
            if (endpoint == null)
            {
                return null;
            }

            return new OpenAIClientOptions
            {
                Endpoint = endpoint
            };
        }

        private static Uri? GetEndpoint(AiProviderType provider)
        {
            return provider switch
            {
                AiProviderType.DeepSeek => DeepSeekEndpoint,
                AiProviderType.MiniMax => MiniMaxEndpoint,
                AiProviderType.Aihubmix => AihubmixEndpoint,
                AiProviderType.OpenRouter => OpenRouterEndpoint,
                AiProviderType.Custom => NormalizeEndpoint(AppConfig.Instance.ConfigData.OpenAIHost),
                _ => null
            };
        }

        private static string GetModel(AiProviderType provider)
        {
            return provider switch
            {
                AiProviderType.OpenAI => OpenAIDefaultModel,
                AiProviderType.DeepSeek => DeepSeekDefaultModel,
                AiProviderType.MiniMax => MiniMaxDefaultModel,
                AiProviderType.Aihubmix => AppConfig.Instance.ConfigData.AihubmixModelName,
                AiProviderType.OpenRouter => AppConfig.Instance.ConfigData.OpenRouterModelName,
                AiProviderType.Custom => string.IsNullOrWhiteSpace(AppConfig.Instance.ConfigData.OpenAIModelName)
                    ? OpenAIDefaultModel
                    : AppConfig.Instance.ConfigData.OpenAIModelName,
                _ => OpenAIDefaultModel
            };
        }

        private static string GetApiKey(AiProviderType? providerOverride = null)
        {
            var provider = providerOverride ?? AppConfig.Instance.ConfigData.AiProvider;
            return provider switch
            {
                AiProviderType.OpenAI => AppConfig.Instance.ConfigData.OpenAIKey,
                AiProviderType.DeepSeek => AppConfig.Instance.ConfigData.DeepSeekKey,
                AiProviderType.MiniMax => AppConfig.Instance.ConfigData.MiniMaxKey,
                AiProviderType.Gemini => AppConfig.Instance.ConfigData.GeminiKey,
                AiProviderType.Aihubmix => AppConfig.Instance.ConfigData.AihubmixKey,
                AiProviderType.OpenRouter => AppConfig.Instance.ConfigData.OpenRouterKey,
                AiProviderType.Custom => AppConfig.Instance.ConfigData.OpenAIKey,
                _ => AppConfig.Instance.ConfigData.OpenAIKey
            };
        }

        private static Uri? NormalizeEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = "https://" + endpoint;
            }

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                return uri;
            }

            return null;
        }
    }
}
