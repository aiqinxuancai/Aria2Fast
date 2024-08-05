using Aria2Fast.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Aria2Fast.View.Contver
{
    public class ImageUrlToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                var task = Task.Run(async () => await ImageCacheUtils.LoadImageAsync(url));
                task.Wait(); // Blocking call to wait for the task completion in this example
                return task.Result;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
