using System;
using System.Globalization;
using System.Windows.Data;
using Aria2Fast.Service;

namespace Aria2Fast.View.Contver
{
    /// <summary>
    /// 判断列表中的 AiConfig 是否为当前生效配置
    /// </summary>
    public class IsSelectedAiConfigConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AiConfig config)
            {
                return AppConfig.Instance.ConfigData.CurrentAiConfig == config;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
