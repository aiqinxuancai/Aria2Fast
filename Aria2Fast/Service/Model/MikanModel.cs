using Aria2Fast.Utils;
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

        //TODO 返回最新的集数int，寻找24小时内更新的字幕组获取集数，并且最新更新时间大于24小时的其他


        public string ImageFull
        {
            get
            {
                return $"{MikanManager.kMikanIndex}{Image}";
            }
        }

        public int UpdateTodayRssCount
        {
            get
            {
                if (Rss == null)
                {
                    return 0;
                }
                return Rss!.Count(a => a.IsUpdateToday);
            }
        }

        /// <summary>
        /// 最新一集的剧集是？
        /// </summary>
        public int NewEpisode
        {
            get
            {
                if (Rss == null)
                {
                    return 0;
                }
                return Rss!.Max(a =>
                {
                    return a.Episode;
                }
                );
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
                return ImageCacheUtils.GetImageWithLocalCache(new Uri(ImageFull));
            }
        }

    }

    public class MikanAnimeRss : BaseNotificationModel
    {
        public string Name { get; set; }
        public string Url { get; set; }

        private List<MikanAnimeRssItem> _items { get; set; }

        private bool _isUpdateToday;
        private int _episode = 0;
        private string _updateTime;

        public List<MikanAnimeRssItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                var item = Items.FirstOrDefault();
                if (item != null)
                {
                    if (int.TryParse(MatchUtils.ExtractEpisodeNumber(item.Title), out int e))
                    {
                        _episode = e;
                    }

                    _isUpdateToday = TimeHelper.IsUpdateToday(item.Updated, +8); ;
                    _updateTime = TimeHelper.FormatTimeAgo(item.Updated, +8);
                }
            }
        }



        public string ShowEpisode
        {
            get
            {
                var text = "";
                var episode = Episode;
                if (episode != 0)
                {
                    text += $"最新集 {episode}";
                }

                return text;
            }
        }

        //TODO 当前集数
        public int Episode
        {
            get 
            {
                return _episode;
            }
        }


        //TODO 最后更新时间
        public string UpdateTime
        {
            get
            {
                return _updateTime;
            }
        }

        public bool IsUpdateToday
        {
            get
            {
                return _isUpdateToday;
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

        //时间格式 “2024/02/29 18:00” 时区是+8
        public string Updated { get; set; }

        public string DownloadLink { get; set; }

        public string MagnetLink { get; set; }
    }

}
