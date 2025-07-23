using System;
using System.Globalization;
using System.Windows.Data;
using Aria2Fast.Service;
using Aria2Fast.Service.Model;

namespace Aria2Fast.View.Contver
{
    public class IsSelectedAria2NodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Aria2Node node)
            {
                return AppConfig.Instance.ConfigData.SelectedRemoteAria2Node == node;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}