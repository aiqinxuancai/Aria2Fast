using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Aria2Fast.View.Contver
{
    public class SubscriptionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 假设您在MikanAnimeRss中有一个布尔属性IsSubscribed来代表是否已订阅
            var isSubscribed = (bool)value;
            return isSubscribed ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
