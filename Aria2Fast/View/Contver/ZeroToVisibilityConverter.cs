using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Aria2Fast.View.Contver
{
    /// <summary>
    /// 数值为 0（如集合 Count 为 0）时显示，否则隐藏
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = 0;
            if (value is int intValue)
            {
                count = intValue;
            }
            else if (value != null && int.TryParse(value.ToString(), out var parsed))
            {
                count = parsed;
            }

            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
