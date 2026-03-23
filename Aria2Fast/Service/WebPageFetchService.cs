using Flurl.Http;
using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    internal static class WebPageFetchService
    {
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        private const int DefaultTimeoutSeconds = 20;
        private const int DefaultMaxChars = 4000;

        public static async Task<WebPageContent?> FetchAsync(string url, int maxChars = DefaultMaxChars)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                var html = await url
                    .WithTimeout(TimeSpan.FromSeconds(DefaultTimeoutSeconds))
                    .WithHeader("User-Agent", "Aria2Fast/1.0")
                    .WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8")
                    .GetStringAsync();

                if (string.IsNullOrWhiteSpace(html))
                {
                    return null;
                }

                var document = new HtmlDocument();
                document.LoadHtml(html);

                RemoveNodes(document, "//script|//style|//noscript|//svg|//iframe|//header|//footer");

                var title = Normalize(document.DocumentNode.SelectSingleNode("//title")?.InnerText);
                var bodyNode = document.DocumentNode.SelectSingleNode("//body") ?? document.DocumentNode;
                var content = Normalize(bodyNode.InnerText);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return null;
                }

                if (content.Length > maxChars)
                {
                    content = content.Substring(0, maxChars);
                }

                return new WebPageContent
                {
                    Url = uri.ToString(),
                    Title = title,
                    Content = content
                };
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Warning($"[WebFetch] 抓取失败：{url} {ex.Message}");
                return null;
            }
        }

        private static void RemoveNodes(HtmlDocument document, string xpath)
        {
            var nodes = document.DocumentNode.SelectNodes(xpath);
            if (nodes == null)
            {
                return;
            }

            foreach (var node in nodes.ToList())
            {
                node.Remove();
            }
        }

        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return WhitespaceRegex.Replace(WebUtility.HtmlDecode(text), " ").Trim();
        }
    }

    internal sealed class WebPageContent
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
