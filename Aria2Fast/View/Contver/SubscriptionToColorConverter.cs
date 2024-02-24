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
            //if (new Random((int)DateTime.Now.Ticks).Next(1, 2) == 1)
            //{
            //    isSubscribed = true;
            //} 
            //else
            //{
            //    isSubscribed = false;
            //}
            var green = (SolidColorBrush)new BrushConverter().ConvertFromString("#19be6b");
            var gray = (SolidColorBrush)new BrushConverter().ConvertFromString("#808695");

            return isSubscribed ? green : gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
