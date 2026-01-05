using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Aria2Fast.View.Contver
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            var parameterString = parameter.ToString();
            if (string.IsNullOrWhiteSpace(parameterString))
            {
                return false;
            }

            if (!Enum.IsDefined(value.GetType(), value))
            {
                return false;
            }

            var enumValue = value.ToString();
            return string.Equals(enumValue, parameterString, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && !boolValue)
            {
                return Binding.DoNothing;
            }

            if (parameter == null)
            {
                return Binding.DoNothing;
            }

            return Enum.Parse(targetType, parameter.ToString());
        }
    }

    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return Visibility.Collapsed;
            }

            var parameterString = parameter.ToString();
            if (string.IsNullOrWhiteSpace(parameterString))
            {
                return Visibility.Collapsed;
            }

            var enumValue = value.ToString();
            return string.Equals(enumValue, parameterString, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
