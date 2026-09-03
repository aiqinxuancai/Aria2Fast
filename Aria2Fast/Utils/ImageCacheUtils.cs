using Flurl.Http;
using Aria2Fast.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
                    //Debug.WriteLine($"[缓存]{cacheKey}");
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
                    else
                    {
                        EasyLogManager.Logger.Error($"[图片] 下载内容为空: {uri}");
                    }
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error($"[图片] 下载失败: {uri}，原因: {ex}");
                }



            }

            try
            {
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                if (img.CanFreeze && !img.IsFrozen) // 检查能否冻结，并且还没有被冻结
                {
                    img.Freeze();
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error($"[图片] 解码失败: {uri}，原因: {ex}");
                return null;
            }

            return img;
        }


        public static async Task<BitmapImage> LoadImageAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        EasyLogManager.Logger.Error($"[图片] HTTP 下载失败: {url}，状态码: {(int)response.StatusCode} {response.ReasonPhrase}");
                        return null;
                    }

                    var imageData = await response.Content.ReadAsByteArrayAsync();
                    if (imageData.Length == 0)
                    {
                        EasyLogManager.Logger.Error($"[图片] 下载内容为空: {url}");
                        return null;
                    }

                    var bitmap = new BitmapImage();
                    using (var stream = new System.IO.MemoryStream(imageData))
                    {
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make the BitmapImage cross-thread accessible
                    }

                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error($"[图片] 加载失败: {url}，原因: {ex}");
                return null;
            }
        }
    }
}
