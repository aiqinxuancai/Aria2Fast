using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aria2Fast.Utils
{
    class MatchUtils
    {
        private static readonly Regex EpisodeRegex = new Regex(@"\[\d{1,3}\]|-\s*\d{1,3}|第\d{1,4}集|第\d{1,4}话|【\d{1,3}】", RegexOptions.Compiled);
        private static readonly Regex CleanUpRegex = new Regex(@"[\[\]\-\s第集话【】]", RegexOptions.Compiled);

        public static string ExtractEpisodeNumber(string title)
        {
            Match match = EpisodeRegex.Match(title);
            if (match.Success)
            {
                return CleanUpRegex.Replace(match.Value, string.Empty);
            }

            return string.Empty;
        }


        public static int GetSeasonFromTitle(string title)
        {
            string pattern = @"第(.{1,2})季";
            Regex regex = new Regex(pattern);

            if (regex.IsMatch(title))
            {
                Match match = regex.Match(title);
                string seasonStr = match.Groups[1].Value;
                // check if the match value is Arabic numeral
                if (int.TryParse(seasonStr, out int seasonNum))
                {
                    return seasonNum;
                }
                // Match Chinese digit
                else
                {
                    switch (seasonStr)
                    {
                        case "一": return 1;
                        case "二": return 2;
                        case "三": return 3;
                        case "四": return 4;
                        case "五": return 5;
                        case "六": return 6;
                        case "七": return 7;
                        case "八": return 8;
                        case "九": return 9;
                        case "十": return 10;
                        default: return 0;
                    }
                }
            }
            return 0;
        }


        public static string RemoveSeasonFromTitle(string title)
        {
            string pattern = @"第.{1,2}季";
            Regex regex = new Regex(pattern);

            if (regex.IsMatch(title))
            {
                return regex.Replace(title, "").Trim();
            }

            return title;
        }
    }
}
