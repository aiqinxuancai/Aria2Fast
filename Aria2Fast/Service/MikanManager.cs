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

                    foreach (var item in weekList)
                    {
                        foreach (var anime in item.Anime)
                        {
                            try
                            {
                                var pageUrl = $"{anime.Url}";
                                var animeHtml = await pageUrl.WithHeader("User-Agent", kUserAgent).WithHeader("Referer", kMikanIndex).GetStringAsync();
                                var animeRss = AnimePage(animeHtml);
                                List<MikanAnimeRss> rssList = JsonConvert.DeserializeObject<List<MikanAnimeRss>>(animeRss);
                                anime.Rss = rssList;
                                Debug.WriteLine(animeRss);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }
                        }
                    }

                    File.WriteAllText(kMikanCacheFile, JsonConvert.SerializeObject(Master.AnimeDays));
                }
                else if (File.Exists(kMikanCacheFile))
                {
                    //读取缓存
                    List<MikanAnimeDay> weekList = JsonConvert.DeserializeObject<List<MikanAnimeDay>>(File.ReadAllText(kMikanCacheFile));
                    Master.AnimeDays = weekList;

                    ImageCacheUtils.PreloadImageCache();
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


        public string AnimePage(string htmlContent)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'subgroup-text')]");
            var subgroupInfoList = new List<object>();

            if (divs == null)
            {
                return "[]";
            }

            foreach (var div in divs)
            {
                var subgroupNames = new List<string>();

                // 检查是否有超链接
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
                    // 检查是否有直接作为文本的字幕组名称
                    var textNode = div.SelectSingleNode(".//text()[normalize-space(.)]");
                    if (textNode != null)
                    {
                        subgroupNames.Add(HtmlEntity.DeEntitize(textNode.InnerText.Trim()));
                    }
                }

                // 将所有找到的名称用 '/' 连接为一个字符串
                var name = string.Join("-", subgroupNames);

                var relativeUrlNode = div.SelectSingleNode(".//a[contains(@class, 'mikan-rss')]");
                if (relativeUrlNode == null)
                {
                    continue; // 如果没有找到rss链接节点则继续下一个循环
                }
                var relativeUrl = relativeUrlNode.GetAttributeValue("href", string.Empty).Trim();
                var absoluteUrl = $"{relativeUrl}";

                // 如果没有找到任何名字，可能需要特殊处理
                if (string.IsNullOrWhiteSpace(name))
                {
                    //TODO: 特殊处理如设置默认值或跳过
                    continue;
                }

                List<object> items = GetRssItems(div);

                subgroupInfoList.Add(new
                {
                    name = name,
                    url = $"{kMikanIndex}{absoluteUrl}",
                    items = items
                });
            }

            return JsonConvert.SerializeObject(subgroupInfoList, Formatting.Indented);
        }

        private static List<object> GetRssItems(HtmlNode div)
        {
            try
            {
                var items = new List<object>();

                // 修改点 1: 现在的 table 被包裹在 class="episode-table" 的 div 中
                // 原代码: var table = div.SelectSingleNode("following-sibling::table[1]");
                var tableContainer = div.SelectSingleNode("following-sibling::div[contains(@class, 'episode-table')][1]");
                var table = tableContainer?.SelectSingleNode(".//table");

                if (table != null)
                {
                    var rows = table.SelectNodes(".//tr[not(ancestor::thead)]");
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            // 修改点 2: 列索引发生了变化。
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
                                items.Add(new
                                {
                                    title = title,
                                    size = size,
                                    updated = updated,
                                    downloadLink = $"{downloadLink}", // 如果这里不是完整链接，可能需要拼接域名，但在 Mikan 通常是相对路径
                                    magnetLink = $"{magnetLink}"
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
                return new List<object>();
            }
        }
    }
}
