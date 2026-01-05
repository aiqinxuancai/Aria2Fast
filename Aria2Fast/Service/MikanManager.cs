using Aria2Fast.Service.Model;
using Aria2Fast.Utils;
using Flurl.Http;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace Aria2Fast.Service
{
    public class MikanManager : BaseNotificationModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //"https://mikanani.me/Home/BangumiCoverFlowByDayOfWeek?year=2023&seasonStr=%E7%A7%8B" ;/

        public const string kMikanIndex = "https://mikanime.tv";

        public const string kMikanCacheFile = "MikanCache.json";

        public const string kUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0";

        private static readonly MikanManager instance = new MikanManager();

        public static MikanManager Instance => instance;

        private MikanAnimeDayMaster master = new MikanAnimeDayMaster();

        public IObservable<Aria2Event> EventReceived => _eventReceivedSubject.AsObservable();
        private readonly Subject<Aria2Event> _eventReceivedSubject = new();

        public MikanAnimeDayMaster Master
        {
            get => master;
            set
            {
                if (master != value)
                {
                    master = value;
                    OnPropertyChanged(nameof(Master));
                }
            }
        }

        public bool IsLoading { set; get; }


        public string SearchText { set; get; }

        // 构造函数私有化确保唯一性
        private MikanManager() { }

        public async Task MikanStart(bool force)
        {
            try
            {
                IsLoading = true;
                
                if (force || !File.Exists(kMikanCacheFile))
                {
                    var indexHtml = await kMikanIndex.WithHeader("User-Agent", kUserAgent).WithHeader("Referer", kMikanIndex).GetStringAsync();
                    var indexJson = IndexPageExtractWeeklyAnimeJson2(indexHtml);
                    Debug.WriteLine(indexJson);

                    //循环将
                    List<MikanAnimeDay> weekList = JsonConvert.DeserializeObject<List<MikanAnimeDay>>(indexJson);

                    Master.AnimeDays = weekList;
                    ImageCacheUtils.PreloadImageCache();

                    Debug.WriteLine(indexJson);

                    // 收集所有动漫到一个列表
                    var allAnime = weekList.SelectMany(day => day.Anime).ToList();
                    Debug.WriteLine($"[Mikan] 开始并发获取 {allAnime.Count} 个动漫页面");

                    // 使用 SemaphoreSlim 控制并发数（建议 5-10 个并发）
                    var semaphore = new SemaphoreSlim(5, 5);
                    var tasks = new List<Task>();

                    foreach (var anime in allAnime)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                var pageUrl = $"{anime.Url}";
                                var animeHtml = await pageUrl.WithHeader("User-Agent", kUserAgent).WithHeader("Referer", kMikanIndex).GetStringAsync();
                                var rssList = AnimePage(animeHtml);
                                anime.Rss = rssList;
                                if (rssList.Count > 0)
                                {
                                    anime.Summary = rssList[0].Summary;
                                }
                                Debug.WriteLine($"[Mikan] 已获取: {anime.Name}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[Mikan] 获取失败 {anime.Name}: {ex.Message}");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }

                    // 等待所有任务完成
                    await Task.WhenAll(tasks);
                    Debug.WriteLine($"[Mikan] 所有页面获取完成");

                    // 后台异步获取 TMDB 信息，不阻塞主流程
                    _ = Task.Run(async () =>
                    {
                        await LoadTmdbInfoInBackground(weekList);
                    });

                    File.WriteAllText(kMikanCacheFile, JsonConvert.SerializeObject(Master.AnimeDays));
                }
                else if (File.Exists(kMikanCacheFile))
                {
                    //读取缓存
                    List<MikanAnimeDay> weekList = JsonConvert.DeserializeObject<List<MikanAnimeDay>>(File.ReadAllText(kMikanCacheFile));
                    Master.AnimeDays = weekList;

                    ImageCacheUtils.PreloadImageCache();

                    if (AppConfig.Instance.ConfigData.OpenAISummaryTranslateOpen)
                    {
                        _ = Task.Run(async () =>
                        {
                            await LoadSummaryTranslationInBackground(weekList);
                        });
                    }

                    if (AppConfig.Instance.ConfigData.OpenAIAiReviewOpen)
                    {
                        _ = Task.Run(async () =>
                        {
                            await LoadAiReviewInBackground(weekList);
                        });
                    }
                }
                else
                {
                    if (force == false)
                    {
                        await MikanStart(true);
                    }
                }
            }
            catch (Exception ex) { 
            
            }
            finally
            {
                IsLoading = false;
                _eventReceivedSubject.OnNext(new MikanListLoaded());
            }
            

        }

        /// <summary>
        /// 从主页获取一个json数据，得到动漫
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public string IndexPageExtractWeeklyAnimeJson(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var weeklyItemNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'm-home-week-item')]");

            var weekList = new List<object>();

            foreach (var weekNode in weeklyItemNodes)
            {
                string title = weekNode.SelectSingleNode(".//div[@class='title']/span")?.InnerText;


                title = HtmlEntity.DeEntitize(title);

                var animeNodes = weekNode.SelectNodes(".//div[contains(@class, 'm-week-square')]");
                var animeList = new List<object>();

                foreach (var animeNode in animeNodes)
                {
                    var name = animeNode.SelectSingleNode(".//a")?.GetAttributeValue("title", string.Empty);
                    name = HtmlEntity.DeEntitize(name);


                    var url = animeNode.SelectSingleNode(".//a")?.GetAttributeValue("href", string.Empty);
                    var imageNode = animeNode.SelectSingleNode(".//img");
                    var imageRelUrl = imageNode?.GetAttributeValue("data-src", string.Empty);
                    var imageAbsUrl = imageRelUrl;

                    imageAbsUrl = imageAbsUrl.Replace("width=400", "width=460");
                    imageAbsUrl = imageAbsUrl.Replace("height=400", "height=640");

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(imageAbsUrl))
                    {
                        animeList.Add(new
                        {
                            name = name,
                            url = $"{kMikanIndex}{url}",
                            image = imageAbsUrl,
                        });
                    }
                }

                if (!string.IsNullOrEmpty(title) && animeList.Count > 0)
                {
                    weekList.Add(new
                    {
                        title = title,
                        anime = animeList,
                    });
                }
            }

            return JsonConvert.SerializeObject(weekList, Formatting.Indented);
        }

        public string IndexPageExtractWeeklyAnimeJson2(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var bangumiNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'sk-bangumi')]");

            var weekList = new List<object>();

            foreach (var dayNode in bangumiNodes)
            {
                var dateNode = dayNode.SelectSingleNode(".//div[starts-with(@id, 'data-row-')]");
                var dateString = dateNode?.InnerText;
                dateString = dateString?.Trim();
                dateString = System.Net.WebUtility.HtmlDecode(dateString);

                var animeNodes = dayNode.SelectNodes(".//li");
                var animeList = new List<object>();

                foreach (var animeNode in animeNodes)
                {
                    var nameNode = animeNode.SelectSingleNode(".//a[@class='an-text']");
                    var name = nameNode?.InnerText;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        // 从最后一个div，class=date-text中提取名称
                        var lastNameNode = animeNode.SelectSingleNode(".//div[@class='an-info-group']/div[contains(@class, 'date-text')][last()]");
                        name = lastNameNode?.InnerText;


                    }


                    name = HtmlEntity.DeEntitize(name);

                    var url = animeNode.SelectSingleNode(".//a[@class='an-text']")?.GetAttributeValue("href", string.Empty);

                    var imageNode = animeNode.SelectSingleNode(".//span");
                    var dataSrc = imageNode?.GetAttributeValue("data-src", string.Empty);
                    var imageUrl = dataSrc;

                    imageUrl = imageUrl.Replace("width=400", "width=460");
                    imageUrl = imageUrl.Replace("height=400", "height=640");

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(imageUrl))
                    {
                        animeList.Add(new
                        {
                            name,
                            url = $"{kMikanIndex}{url}",
                            image = imageUrl
                        });
                    }
                }

                if (dateString != null && animeList.Count > 0)
                {
                    weekList.Add(new
                    {
                        title = dateString,
                        anime = animeList,
                    });
                }
            }

            return JsonConvert.SerializeObject(weekList, Formatting.Indented);
        }


        public List<MikanAnimeRss> AnimePage(string htmlContent)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            string summary = string.Empty;
            // 定位 class="header2-desc" 的 p 标签
            var summaryNode = htmlDoc.DocumentNode.SelectSingleNode("//p[contains(@class, 'header2-desc')]");
            if (summaryNode != null)
            {
                // HtmlEntity.DeEntitize 用于处理 &nbsp; 等特殊字符
                summary = HtmlEntity.DeEntitize(summaryNode.InnerText.Trim());
            }

            var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'subgroup-text')]");
            var subgroupInfoList = new List<MikanAnimeRss>();

            if (divs == null)
            {
                return subgroupInfoList;
            }

            foreach (var div in divs)
            {
                var subgroupNames = new List<string>();

                // 尝试获取直接的链接名称 (旧逻辑保留)
                var anchors = div.SelectNodes(".//a[contains(@class, 'material-dropdown-menu__link')]");
                if (anchors != null)
                {
                    foreach (var anchor in anchors)
                    {
                        subgroupNames.Add(HtmlEntity.DeEntitize(anchor.InnerText.Trim()));
                    }
                }
                else
                {
                    // 修改点 3: 适配新的 HTML 结构,名称通常是 div 下的第一个非 rss class 的 a 标签
                    // HTML 示例: <a href="..." target="_blank" style="...">LoliHouse</a>
                    var nameAnchor = div.SelectSingleNode(".//a[not(contains(@class, 'mikan-rss'))]");
                    if (nameAnchor != null)
                    {
                        subgroupNames.Add(HtmlEntity.DeEntitize(nameAnchor.InnerText.Trim()));
                    }
                    else
                    {
                        // 如果没有 a 标签，再尝试获取纯文本 (兜底)
                        var textNode = div.SelectSingleNode(".//text()[normalize-space(.)]");
                        if (textNode != null)
                        {
                            subgroupNames.Add(HtmlEntity.DeEntitize(textNode.InnerText.Trim()));
                        }
                    }
                }

                // 将所有找到的名称用 '-' 连接为一个字符串
                var name = string.Join("-", subgroupNames);

                var relativeUrlNode = div.SelectSingleNode(".//a[contains(@class, 'mikan-rss')]");
                if (relativeUrlNode == null)
                {
                    continue; // 如果没有找到rss链接节点则继续下一个循环
                }
                var relativeUrl = relativeUrlNode.GetAttributeValue("href", string.Empty).Trim();

                // 如果没有找到任何名字，可能需要特殊处理
                if (string.IsNullOrWhiteSpace(name))
                {
                    //TODO: 特殊处理如设置默认值或跳过
                    continue;
                }

                List<MikanAnimeRssItem> items = GetRssItems(div);

                subgroupInfoList.Add(new MikanAnimeRss
                {
                    Name = name,
                    Url = $"{kMikanIndex}{relativeUrl}",
                    Summary = summary,
                    Items = items
                });
            }

            return subgroupInfoList;
        }

        private static List<MikanAnimeRssItem> GetRssItems(HtmlNode div)
        {
            try
            {
                var items = new List<MikanAnimeRssItem>();

                var tableContainer = div.SelectSingleNode("following-sibling::div[contains(@class, 'episode-table')][1]");
                var table = tableContainer?.SelectSingleNode(".//table");

                if (table != null)
                {
                    var rows = table.SelectNodes(".//tr[not(ancestor::thead)]");
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
  
                            // 新结构: 
                            // td[1]: Checkbox
                            // td[2]: 标题 (a.magnet-link-wrap) 和 磁力链接按钮
                            // td[3]: 大小
                            // td[4]: 更新时间
                            // td[5]: 下载链接 (.torrent)

                            // 获取标题 (注意：现在标题在 td[2] 下的 a 标签中)
                            var titleNode = row.SelectSingleNode(".//td[2]/a[contains(@class, 'magnet-link-wrap')]");
                            var title = HtmlEntity.DeEntitize(titleNode?.InnerText.Trim() ?? "");

                            // 获取大小 (td[3])
                            var sizeNode = row.SelectSingleNode(".//td[3]");
                            var size = HtmlEntity.DeEntitize(sizeNode?.InnerText.Trim() ?? "");

                            // 获取更新时间 (td[4])
                            var updatedNode = row.SelectSingleNode(".//td[4]");
                            var updated = HtmlEntity.DeEntitize(updatedNode?.InnerText.Trim() ?? "");

                            // 获取下载链接 (td[5])
                            var downloadLinkNode = row.SelectSingleNode(".//td[5]/a");
                            var downloadLink = downloadLinkNode?.GetAttributeValue("href", string.Empty).Trim();

                            // 获取磁力链接 (在 td[2] 下的另一个 a 标签中)
                            var magnetLinkNode = row.SelectSingleNode(".//td[2]/a[contains(@class, 'js-magnet')]");
                            var magnetLink = magnetLinkNode?.GetAttributeValue("data-clipboard-text", string.Empty).Trim();

                            // 只有当标题不为空时才添加，避免添加空行
                            if (!string.IsNullOrEmpty(title))
                            {
                                items.Add(new MikanAnimeRssItem
                                {
                                    Title = title,
                                    Size = size,
                                    Updated = updated,
                                    DownloadLink = downloadLink ?? string.Empty,
                                    MagnetLink = magnetLink ?? string.Empty
                                });
                            }
                        }
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetRssItems Error: " + ex.Message);
                return new List<MikanAnimeRssItem>();
            }
        }

        /// <summary>
        /// 后台加载 TMDB 信息
        /// </summary>
        private async Task LoadTmdbInfoInBackground(List<MikanAnimeDay> weekList)
        {
            try
            {
                Debug.WriteLine("[TMDB] 开始后台加载 TMDB 信息");
                int loadedCount = 0;
                int totalCount = weekList.Sum(day => day.Anime.Count);

                foreach (var item in weekList)
                {
                    foreach (var anime in item.Anime)
                    {
                        try
                        {
                            // 先检查缓存
                            var tmdbInfo = await TmdbManager.Instance.SearchAnimeAsync(anime.Name, useCache: true);
                            if (tmdbInfo != null)
                            {
                                anime.TmdbInfo = tmdbInfo;
                                loadedCount++;

                                // 通知 UI 更新（触发属性变更）
                                anime.OnPropertyChanged(nameof(anime.TmdbInfo));
                                anime.OnPropertyChanged(nameof(anime.BestSummary));

                                Debug.WriteLine($"[TMDB] 已加载 {loadedCount}/{totalCount}: {anime.Name}");
                            }

                            if (tmdbInfo == null || (string.IsNullOrWhiteSpace(tmdbInfo.OverviewZh) && string.IsNullOrWhiteSpace(tmdbInfo.OverviewEn)))
                            {
                                await TryTranslateAnimeSummaryAsync(anime);
                            }

                            await TryGenerateAiReviewAsync(anime);

                            // 避免请求过快，稍微延迟
                            await Task.Delay(250);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[TMDB] 获取 {anime.Name} 信息失败: {ex.Message}");
                        }
                    }
                }

                Debug.WriteLine($"[TMDB] 后台加载完成，成功加载 {loadedCount}/{totalCount} 条");

                // 保存更新后的缓存
                try
                {
                    File.WriteAllText(kMikanCacheFile, JsonConvert.SerializeObject(Master.AnimeDays));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TMDB] 保存缓存失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TMDB] 后台加载失败: {ex.Message}");
            }
        }

        private static async Task TryTranslateAnimeSummaryAsync(MikanAnime anime)
        {
            if (!AppConfig.Instance.ConfigData.OpenAISummaryTranslateOpen)
            {
                return;
            }

            if (anime == null || string.IsNullOrWhiteSpace(anime.Summary) || !string.IsNullOrWhiteSpace(anime.SummaryTranslated))
            {
                return;
            }

            if (anime.TmdbInfo != null && (!string.IsNullOrWhiteSpace(anime.TmdbInfo.OverviewZh) || !string.IsNullOrWhiteSpace(anime.TmdbInfo.OverviewEn)))
            {
                return;
            }

            if (AnimeSummaryTranslatorManager.TryGetCachedTranslation(anime.Summary, out var cached))
            {
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    anime.SummaryTranslated = cached;
                    anime.OnPropertyChanged(nameof(anime.SummaryTranslated));
                    anime.OnPropertyChanged(nameof(anime.BestSummary));
                }
                return;
            }

            if (!AiProviderClient.HasApiKey())
            {
                return;
            }

            if (!AnimeSummaryTranslatorManager.IsLikelyJapanese(anime.Summary))
            {
                return;
            }

            var translated = await AnimeSummaryTranslatorManager.TranslateToChineseAsync(anime.Summary);
            if (!string.IsNullOrWhiteSpace(translated))
            {
                anime.SummaryTranslated = translated;
                anime.OnPropertyChanged(nameof(anime.SummaryTranslated));
                anime.OnPropertyChanged(nameof(anime.BestSummary));
            }
        }

        private static async Task TryGenerateAiReviewAsync(MikanAnime anime)
        {
            if (!AppConfig.Instance.ConfigData.OpenAIAiReviewOpen)
            {
                return;
            }

            if (!AiProviderClient.HasApiKey())
            {
                return;
            }

            if (anime == null || !string.IsNullOrWhiteSpace(anime.AiReviewText))
            {
                return;
            }

            var summary = anime.TmdbInfo?.OverviewZh;
            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = anime.TmdbInfo?.OverviewEn;
            }
            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = anime.SummaryTranslated;
            }
            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = anime.Summary;
            }

            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(anime.Name))
            {
                return;
            }

            var item = await AnimeAiReviewManager.GetReviewAsync(anime.Name ?? string.Empty, summary ?? string.Empty, anime.TmdbInfo);
            if (item != null && !string.IsNullOrWhiteSpace(item.Review))
            {
                anime.AiReviewText = item.Review;
                anime.AiReviewScore = item.Score;
                anime.OnPropertyChanged(nameof(anime.AiReviewText));
                anime.OnPropertyChanged(nameof(anime.AiReviewScore));
            }
        }

        private async Task LoadAiReviewInBackground(List<MikanAnimeDay> weekList)
        {
            try
            {
                Debug.WriteLine("[AI] 开始后台生成动漫简评");
                int totalCount = weekList.Sum(day => day.Anime.Count);
                int reviewedCount = 0;

                foreach (var item in weekList)
                {
                    foreach (var anime in item.Anime)
                    {
                        await TryGenerateAiReviewAsync(anime);
                        if (!string.IsNullOrWhiteSpace(anime.AiReviewText))
                        {
                            reviewedCount++;
                        }

                        await Task.Delay(250);
                    }
                }

                Debug.WriteLine($"[AI] 后台简评完成，已处理 {reviewedCount}/{totalCount} 条");

                try
                {
                    File.WriteAllText(kMikanCacheFile, JsonConvert.SerializeObject(Master.AnimeDays));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AI] 保存缓存失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AI] 后台简评失败: {ex.Message}");
            }
        }

        private async Task LoadSummaryTranslationInBackground(List<MikanAnimeDay> weekList)
        {
            try
            {
                Debug.WriteLine("[AI] 开始后台翻译动漫简介");
                int totalCount = weekList.Sum(day => day.Anime.Count);
                int translatedCount = 0;

                foreach (var item in weekList)
                {
                    foreach (var anime in item.Anime)
                    {
                        await TryTranslateAnimeSummaryAsync(anime);
                        if (!string.IsNullOrWhiteSpace(anime.SummaryTranslated))
                        {
                            translatedCount++;
                        }

                        await Task.Delay(250);
                    }
                }

                Debug.WriteLine($"[AI] 后台翻译完成，已处理 {translatedCount}/{totalCount} 条");

                try
                {
                    File.WriteAllText(kMikanCacheFile, JsonConvert.SerializeObject(Master.AnimeDays));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AI] 保存缓存失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AI] 后台翻译失败: {ex.Message}");
            }
        }
    }
}
