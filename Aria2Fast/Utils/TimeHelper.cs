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
        public static string ConvertToTimeAgo(DateTime dateTime)
        {
            var now = DateTime.Now;
            var timeSpan = now - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.Seconds}秒前";

            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes}分钟前";

            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours}小时前";

            if (timeSpan.TotalDays < 30)
                return $"{timeSpan.Days}天前";

            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}月前";

            return $"{(int)(timeSpan.TotalDays / 365)}年前";
        }


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
