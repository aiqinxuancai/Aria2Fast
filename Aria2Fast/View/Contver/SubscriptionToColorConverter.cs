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
        private static SolidColorBrush _green = (SolidColorBrush)new BrushConverter().ConvertFromString("#65B741");
        private static SolidColorBrush _gray = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5E1DA");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSubscribed = (bool)value;
            return isSubscribed ? _green : _gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SubscriptionToColorConverterWithTitle : IValueConverter
    {
        private static SolidColorBrush _green = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");
        private static SolidColorBrush _gray = (SolidColorBrush)new BrushConverter().ConvertFromString("#666666");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSubscribed = (bool)value;
            return isSubscribed ? _green : _gray;
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
