using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service.Model
{
    public class MikanAnimeDayMaster
    {
        public List<MikanAnimeDay> AnimeDays { get; set; }
    }

    /// <summary>
    /// 主节点 使用数组形式List<MikanAnimeDay>
    /// </summary>
    public class MikanAnimeDay
    {
        public string Title { get; set; }
        public List<MikanAnime> Anime { get; set; }
    }

    public class MikanAnime
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }

        public List<MikanAnimeRss> Rss { get; set; }
    }

    public class MikanAnimeRss
    {
        public string Name { get; set; }
        public string Url { get; set; }

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

}
