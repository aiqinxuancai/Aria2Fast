﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
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
            MemoryCache cache = MemoryCache.Default;

            await Task.Run(() => {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    string cacheKey = fileName;
                    if (!cache.Contains(cacheKey))
                    {

                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.DelayCreation;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(file);
                        bitmap.EndInit();

                        bitmap.Freeze();

                        cache.Set(cacheKey, bitmap, new CacheItemPolicy());
                        Debug.WriteLine($"[缓存]{cacheKey}");

                    }
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
            MemoryCache cache = MemoryCache.Default;
            var fileName = Path.GetFileName(uri.LocalPath);
            if (cache.Contains(fileName))
            {
                var bmp = (BitmapImage)cache.Get(fileName);
                //bmp.Freeze();
                return bmp;
            }

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
                img.UriSource = uri;
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
            img.Freeze();
            return img;
        }

        public static BitmapImage GetImageFromCache(Uri uri)
        {
            BitmapImage img = null;

            MemoryCache cache = MemoryCache.Default;
           
            var fileName = Path.GetFileName(uri.LocalPath);
            string cacheKey = fileName;

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
}
