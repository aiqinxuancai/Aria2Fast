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
        private static readonly Regex EpisodeRegex = new Regex(@"\[\d{1,3}\]|-\s*\d{1,3}|第\d{1,4}集|第\d{1,4}话", RegexOptions.Compiled);
        private static readonly Regex CleanUpRegex = new Regex(@"[\[\]\-\s第集话]", RegexOptions.Compiled);

        public static string ExtractEpisodeNumber(string title)
        {
            Match match = EpisodeRegex.Match(title);
            if (match.Success)
            {
                return CleanUpRegex.Replace(match.Value, string.Empty);
            }

            return string.Empty;
        }
    }
}
