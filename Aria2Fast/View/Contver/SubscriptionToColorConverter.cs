using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Aria2Fast.View.Contver
{
    public class SubscriptionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSubscribed = (bool)value;
            return isSubscribed
                ? BrushResourceHelper.GetBrush("App.SubscriptionActiveBrush", "#40A02B")
                : BrushResourceHelper.GetBrush("App.SubscriptionInactiveBrush", "#51576D");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SubscriptionToColorConverterWithTitle : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSubscribed = (bool)value;
            return isSubscribed
                ? BrushResourceHelper.GetBrush("App.SubscriptionActiveTextBrush", "#EFF1F5")
                : BrushResourceHelper.GetBrush("App.SubscriptionInactiveTextBrush", "#A5ADCE");
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

    internal static class BrushResourceHelper
    {
        internal static SolidColorBrush GetBrush(string key, string fallbackHex)
        {
            if (Application.Current?.Resources[key] is SolidColorBrush brush)
            {
                return brush;
            }

            if (Application.Current?.Resources[key] is Color color)
            {
                return new SolidColorBrush(color);
            }

            return (SolidColorBrush)new BrushConverter().ConvertFromString(fallbackHex);
        }
    }
}
