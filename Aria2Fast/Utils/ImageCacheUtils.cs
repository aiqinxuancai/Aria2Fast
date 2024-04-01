using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Aria2Fast.Utils
{
    class ImageCacheUtils
    {
        //TODO 预加载图片缓存

        public static async Task PreloadImageCache()
        {
            var dirPath = Path.Combine(Directory.GetCurrentDirectory(), "ImageCached");
            var files = Directory.GetFiles(dirPath);
            //MemoryCache cache = MemoryCache.Default;

            await Task.Run(() => {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    string cacheKey = fileName;
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.DelayCreation;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(file);
                    bitmap.EndInit();

                    bitmap.Freeze();

                    //cache.Set(cacheKey, bitmap, new CacheItemPolicy());
                    Debug.WriteLine($"[缓存]{cacheKey}");
                }
            });


        }

        /// <summary>
        /// 优先缓存，否则才进这里
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static BitmapImage GetImageWithLocalCache(Uri uri)
        {
            //MemoryCache cache = MemoryCache.Default;
            var fileName = Path.GetFileName(uri.LocalPath);
            //if (cache.Contains(fileName))
            //{
            //    var bmp = (BitmapImage)cache.Get(fileName);
            //    //bmp.Freeze();
            //    return bmp;
            //}

            var dirPath = Path.Combine(Directory.GetCurrentDirectory(), "ImageCached");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var localFilePath = Path.Combine(dirPath, fileName);

            BitmapImage img = new BitmapImage();
            img.BeginInit();
            if (File.Exists(localFilePath))
            {
                img.UriSource = new Uri(localFilePath);
            }
            else
            {
                try
                {
                    var result = uri.GetBytesAsync().Result;
                    if (result != null && result.Length > 0)
                    {
                        img.StreamSource = new MemoryStream(result);
                        File.WriteAllBytes(localFilePath, result);
                    }
                }
                catch (Exception ex)
                {

                }



            }

            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            if (img.CanFreeze && !img.IsFrozen) // 检查能否冻结，并且还没有被冻结
            {
                img.Freeze();
            }
            
            return img;
        }

    }
}
