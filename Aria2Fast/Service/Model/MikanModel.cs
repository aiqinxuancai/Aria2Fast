using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
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

        public List<MikanAnimeRss> Rss { get; set; }




        public string ImageFull
        {
            get
            {
                return $"{MikanManager.kMikanIndex}{Image}" ;
            }
        }
    }

    public class MikanAnimeRss : BaseNotificationModel
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public List<MikanAnimeRssItem> Items { get; set; }

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
