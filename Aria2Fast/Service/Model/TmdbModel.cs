using System;
using System.Collections.Generic;

namespace Aria2Fast.Service.Model
{
    /// <summary>
    /// TMDB 动漫信息
    /// </summary>
    public class TmdbAnimeInfo
    {
        /// <summary>
        /// TMDB ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 动漫名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 原始名称
        /// </summary>
        public string OriginalName { get; set; } = string.Empty;

        /// <summary>
        /// 中文简介
        /// </summary>
        public string OverviewZh { get; set; } = string.Empty;

        /// <summary>
        /// 英文简介
        /// </summary>
        public string OverviewEn { get; set; } = string.Empty;

        /// <summary>
        /// 评分 (0-10)
        /// </summary>
        public double VoteAverage { get; set; }

        /// <summary>
        /// 评分人数
        /// </summary>
        public int VoteCount { get; set; }

        /// <summary>
        /// 首播日期
        /// </summary>
        public string FirstAirDate { get; set; } = string.Empty;

        /// <summary>
        /// 海报路径
        /// </summary>
        public string PosterPath { get; set; } = string.Empty;

        /// <summary>
        /// 背景图路径
        /// </summary>
        public string BackdropPath { get; set; } = string.Empty;

        /// <summary>
        /// 获取最佳简介（优先中文，其次英文）
        /// </summary>
        public string GetBestOverview()
        {
            if (!string.IsNullOrWhiteSpace(OverviewZh))
            {
                return OverviewZh;
            }
            return OverviewEn;
        }

        /// <summary>
        /// 获取完整海报 URL
        /// </summary>
        public string GetPosterUrl(string size = "w500")
        {
            if (string.IsNullOrWhiteSpace(PosterPath))
            {
                return string.Empty;
            }
            return $"https://image.tmdb.org/t/p/{size}{PosterPath}";
        }

        /// <summary>
        /// 获取完整背景图 URL
        /// </summary>
        public string GetBackdropUrl(string size = "w1280")
        {
            if (string.IsNullOrWhiteSpace(BackdropPath))
            {
                return string.Empty;
            }
            return $"https://image.tmdb.org/t/p/{size}{BackdropPath}";
        }

        /// <summary>
        /// 缓存时间
        /// </summary>
        public DateTime CachedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// TMDB 缓存数据
    /// </summary>
    public class TmdbCache
    {
        /// <summary>
        /// 键：动漫名称，值：TMDB 信息
        /// </summary>
        public Dictionary<string, TmdbAnimeInfo> Cache { get; set; } = new Dictionary<string, TmdbAnimeInfo>();

        /// <summary>
        /// 缓存版本
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
