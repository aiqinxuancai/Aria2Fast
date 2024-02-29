using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Aria2Fast.Service.Model
{
    public class MikanAnimeDayMaster : BaseNotificationModel
    {
        public List<MikanAnimeDay> AnimeDays { get; set; }
    }

    /// <summary>
    /// 主节点 使用数组形式List<MikanAnimeDay>
    /// </summary>
    public class MikanAnimeDay : BaseNotificationModel
    {
        public string Title { get; set; }
        public List<MikanAnime> Anime { get; set; }
    }

    public class MikanAnime : BaseNotificationModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }

        public List<MikanAnimeRss> Rss { get; set; }


        //当前集数


        public string ImageFull
        {
            get
            {
                return $"{MikanManager.kMikanIndex}{Image}";
            }
        }

        /// <summary>
        /// 和搜索联动的代码
        /// </summary>
        public Visibility ShowStatus
        {
            get
            {

                if (string.IsNullOrWhiteSpace(MikanManager.Instance.SearchText))
                {
                    return Visibility.Visible;
                }
                if (Name.Contains(MikanManager.Instance.SearchText))
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public BitmapImage ImageCache
        {
            get
            {
                return GetImageWithLocalCache(new Uri(ImageFull));
            }
        }

        public static BitmapImage GetImageWithLocalCache(Uri uri)
        {
            var fileName = Path.GetFileName(uri.LocalPath);

            var dirPath = Path.Combine(AppContext.BaseDirectory, "ImageCached");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var localFilePath = Path.Combine(dirPath, fileName);

            BitmapImage img = new BitmapImage();
            img.BeginInit();

            // Check if the image is cached locally first
            if (File.Exists(localFilePath))
            {
                // Use the local cache file
                img.UriSource = new Uri(localFilePath);
            }
            else
            {
                // Download the image from the internet
                img.UriSource = uri;

                // Save the image to the local cache once it's downloaded
                img.DownloadCompleted += (s, e) =>
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapImage)s));
                    using (var fileStream = new FileStream(localFilePath, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                };
            }
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();

            return img;
        }

        public BitmapImage GetImageFromCache(Uri uri)
        {
            BitmapImage img = null;

            MemoryCache cache = MemoryCache.Default;
            string cacheKey = uri.AbsoluteUri;

            if (cache.Contains(cacheKey))
            {
                img = (BitmapImage)cache.Get(cacheKey);
            }
            else
            {
                img = new BitmapImage(uri);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.DownloadCompleted += (s, e) =>
                {
                    cache.Set(cacheKey, img, new CacheItemPolicy());
                };
            }

            return img;
        }


    }

    public class MikanAnimeRss : BaseNotificationModel
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public List<MikanAnimeRssItem> Items { get; set; }

        public string ShowEpisode
        {
            get
            {
                var text = "";
                var episode = Episode;
                if (!string.IsNullOrEmpty(episode))
                {
                    text += $"最新集数 {episode}";
                }

                return text;
                // text += $" 总{Items.Count}";
            }
        }

        //TODO 当前集数
        public string Episode
        {
            get 
            {
                var item = Items.FirstOrDefault();
                if (item != null)
                {
                    return ExtractEpisodeNumber(item.Title);
                }
                return string.Empty;

            }
        }
        public static string ExtractEpisodeNumber(string title)
        {
            //[01], - 02, 第03集
            Regex episodeRegex = new Regex(@"\[\d{1,3}\]|-\s*\d{1,3}|第\d{1,4}集", RegexOptions.Compiled);

            Match match = episodeRegex.Match(title);
            if (match.Success)
            {
                return Regex.Replace(match.Value, @"[\[\]\-\s第集]", "");
            }

            return "";
        }

        //TODO 最后更新时间
        public string UpdateTime
        {
            get
            {
                var item = Items.FirstOrDefault();
                if (item != null)
                {

                    string result = FormatTimeAgo(item.Updated, +8);

                    return result;
                }
                return string.Empty;
            }
        }


        

        public static string FormatTimeAgo(string timeText, int timeZoneOffset)
        {
            try
            {
                // 解析时间字符串
                DateTime inputTime = DateTime.ParseExact(timeText, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

                // 考虑时区转换到UTC
                inputTime = inputTime.AddHours(-timeZoneOffset);

                // 获取当前的UTC时间
                DateTime utcNow = DateTime.UtcNow;

                // 计算差值
                TimeSpan difference = utcNow - inputTime;

                if (difference.TotalDays >= 1)
                {
                    return timeText;
                }
                else if (difference.TotalHours >= 1)
                {
                    return $"{Math.Floor(difference.TotalHours)}小时前";
                }
                else if (difference.TotalMinutes >= 1)
                {
                    return $"{Math.Floor(difference.TotalMinutes)}分钟前";
                }
                else
                {
                    return "刚刚";
                }
            }
            catch (FormatException)
            {
                return "";
            }
        }

        public bool IsSubscribed
        {
            get
            {
                if (SubscriptionManager.Instance.SubscriptionModel.Any(a => a.Url.Contains(Url))) 
                {
                    return true; 
                }
                return false;
            }
        }
    }

    public class MikanAnimeRssItem : BaseNotificationModel
    {
        public string Title { get; set; }

        public string Size { get; set; }

        public string Updated { get; set; }

        public string DownloadLink { get; set; }

        public string MagnetLink { get; set; }
    }

}
