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
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    public class MikanManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public const string kMikanIndex = "https://mikanani.me";

        public const string kMikanCacheFile = "MikanCache.json";


        private static readonly MikanManager instance = new MikanManager();

        public static MikanManager Instance => instance;

        private MikanAnimeDayMaster master = new MikanAnimeDayMaster();
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

        // 构造函数私有化确保唯一性
        private MikanManager() { }

        public async Task MikanStart(bool force)
        {
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
                var name = string.Join("/", subgroupNames);

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

                subgroupInfoList.Add(new { name = name, url = $"{kMikanIndex}{absoluteUrl}" });
            }

            return JsonConvert.SerializeObject(subgroupInfoList, Formatting.Indented);
        }
    }
}
