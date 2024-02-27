using Aria2Fast.Service.Model;
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

        public const string kMikanIndex = "https://mikanime.tv";

        public const string kMikanCacheFile = "MikanCache.json";


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
                    var indexHtml = await kMikanIndex.GetStringAsync();
                    var indexJson = IndexPageExtractWeeklyAnimeJson(indexHtml);
                    Debug.WriteLine(indexJson);

                    //循环将
                    List<MikanAnimeDay> weekList = JsonConvert.DeserializeObject<List<MikanAnimeDay>>(indexJson);

                    Master.AnimeDays = weekList;

                    Debug.WriteLine(indexJson);

                    foreach (var item in weekList)
                    {
                        foreach (var anime in item.Anime)
                        {
                            try
                            {
                                var pageUrl = $"{anime.Url}";
                                var animeHtml = await pageUrl.GetStringAsync();
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
                    imageAbsUrl = imageAbsUrl.Replace("height=400", "width=640");

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


        public string AnimePage(string htmlContent)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'subgroup-text')]");
            var subgroupInfoList = new List<object>();

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
                var table = div.SelectSingleNode("following-sibling::table[1]"); // 获取当前 div 下方的第一个 table
                if (table != null)
                {
                    // 仅获取所有的 tr，不特定 tbody
                    var rows = table.SelectNodes(".//tr[not(ancestor::thead)]"); // 获取所有不属于 thead 祖先的 tr 元素
                    if (rows != null)
                    {
                        // 从索引1开始遍历，跳过表头行
                        for (int i = 0; i < rows.Count; i++)
                        {
                            var row = rows[i];
                            // 下面的代码与您原来的保持一致
                            var title = HtmlEntity.DeEntitize(row.SelectSingleNode(".//td[1]").InnerText.Trim());
                            var size = HtmlEntity.DeEntitize(row.SelectSingleNode(".//td[2]").InnerText.Trim());
                            var updated = HtmlEntity.DeEntitize(row.SelectSingleNode(".//td[3]").InnerText.Trim());
                            var downloadLink = row.SelectSingleNode(".//td[4]/a").GetAttributeValue("href", string.Empty).Trim();
                            var magnetLink = row.SelectSingleNode(".//td[1]/a[@class='js-magnet magnet-link']").GetAttributeValue("data-clipboard-text", string.Empty).Trim();

                            items.Add(new
                            {
                                title = title,
                                size = size,
                                updated = updated,
                                downloadLink = $"{downloadLink}",
                                magnetLink = $"{magnetLink}"
                            });
                        }
                    }
                }

                return items;
            } 
            catch { 
            
                return new List<object>();
            }

        }
    }
}
