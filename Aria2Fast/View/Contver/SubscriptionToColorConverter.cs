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
            var isSubscribed = (bool)value;
            var green = (SolidColorBrush)new BrushConverter().ConvertFromString("#19be6b");
            var gray = (SolidColorBrush)new BrushConverter().ConvertFromString("#88808695");

            return isSubscribed ? green : gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WidthMinusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string deduction)
            {
                return width - double.Parse(deduction);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
