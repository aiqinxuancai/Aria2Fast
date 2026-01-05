using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Aria2Fast.View.Contver
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            // 如果是布尔类型，检查其值
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            // 对于其他对象类型，只要不为 null 就显示
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(uint), typeof(Visibility))]
    public class IntVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? number = (int?)value;

            if (number > 0)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;

            if (!string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class EmptyStringVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;

            if (string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class InRangeToVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// 检查一个值是否在指定的范围内 [min, max]。
        /// </summary>
        /// <param name="values">
        /// 绑定值的数组，预期顺序为:
        /// values[0]: 要检查的当前值 (e.g., UpdateTodayRssCount)
        /// values[1]: 范围的最小值 (>=)
        /// values[2]: 范围的最大值 (<=)
        /// </param>
        /// <param name="targetType"></param>
        /// <param name="parameter">额外的静态参数 (可选)</param>
        /// <param name="culture"></param>
        /// <returns>如果在范围内，返回 Visible；否则返回 Collapsed。</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 确保我们有三个有效的绑定值
            if (values == null || values.Length < 3 || values.Any(v => v == DependencyProperty.UnsetValue))
            {
                return Visibility.Collapsed;
            }

            try
            {
                // 将绑定值转换为 double 以支持整数和浮点数
                double currentValue = System.Convert.ToDouble(values[0]);
                double minValue = System.Convert.ToDouble(values[1]);
                double maxValue = System.Convert.ToDouble(values[2]);

                // 执行核心逻辑：检查 currentValue 是否在 [minValue, maxValue] 之间
                if (currentValue >= minValue && currentValue <= maxValue)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // 如果类型转换失败，打印调试信息并返回默认值
                System.Diagnostics.Debug.WriteLine($"InRangeToVisibilityConverter Error: {ex.Message}");
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // 单向绑定不需要实现 ConvertBack
            throw new NotImplementedException();
        }
    }
}
