﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Aria2Fast.Service;
using Aria2Fast.Utils;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Aria2Fast.View.Contver
{
    [ValueConversion(typeof(int), typeof(string))]
    public class DownloadSizeContver : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                var price = FileSizeContver.GetSizeString(long.Parse((string)value));
                return price;
            }
            if (value is long)
            {
                var price = FileSizeContver.GetSizeString((long)value);
                return price;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }


    }

    [ValueConversion(typeof(long), typeof(string))]
    public class DownloadStatusContver : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            return value switch
            {
                Aria2ApiManager.KARIA2_STATUS_ACTIVE => "下载中",
                Aria2ApiManager.KARIA2_STATUS_WAITING => "等待中",
                Aria2ApiManager.KARIA2_STATUS_PAUSED => "已暂停",
                Aria2ApiManager.KARIA2_STATUS_ERROR => "错误",
                Aria2ApiManager.KARIA2_STATUS_COMPLETE => "已完成",
                Aria2ApiManager.KARIA2_STATUS_REMOVED => "已删除",
                _ => value,


                //0 => "添加中",
                //8 => "等待中",
                //9 => "已暂停",
                //1 => "下载中",
                //11 => "已完成",
                //12 => "缺少资源",
                //14 => "准备添加中",
                //38 => "磁盘写入异常",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    [ValueConversion(typeof(long), typeof(SolidColorBrush))]
    public class DownloadStatusBrushContver : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //ColorConverter colorConverter = new ColorConverter();

            var color = value switch
            {
                Aria2ApiManager.KARIA2_STATUS_ACTIVE => (Color)ColorConverter.ConvertFromString("#2d8cf0"), //"添加中", //蓝色
                Aria2ApiManager.KARIA2_STATUS_WAITING => (Color)ColorConverter.ConvertFromString("#2d8cf0"),//"等待中", //蓝色
                Aria2ApiManager.KARIA2_STATUS_PAUSED => (Color)ColorConverter.ConvertFromString("#ff9900"),//"已暂停", //橙色
                Aria2ApiManager.KARIA2_STATUS_ERROR => (Color)ColorConverter.ConvertFromString("#ed4014"),//"缺少资源", //红色
                Aria2ApiManager.KARIA2_STATUS_COMPLETE => (Color)ColorConverter.ConvertFromString("#65B741"), //已完成 //绿色
                //Aria2ApiManager.KARIA2_STATUS_REMOVED => "removed",

                _ => (Color)ColorConverter.ConvertFromString("#f8f8f9"), //value, //灰色
            };


            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }




    /// <summary>
    /// 当前下载进度转换
    /// </summary>
    public class DownloadProgressContver : IMultiValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="values">参数1进度 万分之，参数2 总大小</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            long fullSize = 0;
            double progress = 0;
            if (values.Length == 2)
            {
                fullSize = (long)values[0];
            }


            return FileSizeContver.GetSizeString(fullSize);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DownloadStatusFullContver : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = values[0] as string;
            var errorCode = values[1] as string;



            if (value == null)
            {
                return "错误";
            }


            var errorTitle = value switch
            {
                Aria2ApiManager.KARIA2_STATUS_ACTIVE => "下载中",
                Aria2ApiManager.KARIA2_STATUS_WAITING => "等待中",
                Aria2ApiManager.KARIA2_STATUS_PAUSED => "已暂停",
                Aria2ApiManager.KARIA2_STATUS_ERROR => "错误",
                Aria2ApiManager.KARIA2_STATUS_COMPLETE => "已完成",
                Aria2ApiManager.KARIA2_STATUS_REMOVED => "已删除",
                _ => value,


                //0 => "添加中",
                //8 => "等待中",
                //9 => "已暂停",
                //1 => "下载中",
                //11 => "已完成",
                //12 => "缺少资源",
                //14 => "准备添加中",
                //38 => "磁盘写入异常",
            };
            return errorTitle;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorStatusEasyContver : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = values[0] as string;
            var errorCode = values[1] as string;
        
            return value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorStatusEasyNeedShowContver : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = values[0] as string;
            var errorCode = values[1] as string;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(uint), typeof(Visibility))]
    public class DownloadProgressVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((string)value == Aria2ApiManager.KARIA2_STATUS_ACTIVE)
            {
                return Visibility.Visible;
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(uint), typeof(Visibility))]
    public class DownloadSpeedVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                Aria2ApiManager.KARIA2_STATUS_ACTIVE => Visibility.Visible,
                _ => Visibility.Collapsed
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(uint), typeof(Visibility))]
    public class ExistVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                bool? seed = (bool?)values[0];
                //string? seed = (bool?)values[0];

                //if ((long)values[0] == 0 && (long)values[1] == (long)TaskState.Completed) //TODO
                //{
                //    return Visibility.Visible;
                //}
                if (seed == true)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(uint), typeof(double))]
    public class DownloadSizeToProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                double v1 = (long)values[0];
                double v2 = (long)values[1];

                if (v2 == 0)
                {
                    return 0d;
                }
                double result = (double)(v1 / v2 * 10000);

                if (result > 0 && result  < 10000)
                {

                }

                return result;
            }

            return 0d;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

   
    


    public class NegationConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
