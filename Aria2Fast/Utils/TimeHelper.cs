using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Utils
{
    class TimeHelper
    {

        public static string SecondsToFormatString(int seconds)
        {
            TimeSpan ts = new TimeSpan(0, 0, seconds);
            string r = string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
            return r;
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


        public static bool IsUpdateToday(string timeText, int timeZoneOffset)
        {
            try
            {
                DateTime inputTime = DateTime.ParseExact(timeText, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                inputTime = inputTime.AddHours(-timeZoneOffset);
                DateTime utcNow = DateTime.UtcNow;
                TimeSpan difference = utcNow - inputTime;

                if (difference.TotalDays < 1)
                {
                    return true;
                }
            }
            catch (FormatException)
            {

            }
            return false;
        }
    }
}
