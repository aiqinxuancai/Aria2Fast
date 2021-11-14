﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WkyFast.Service;
using WkyFast.Utils;

namespace WkyFast.Window
{
    /// <summary>
    /// WindowAddTask.xaml 的交互逻辑
    /// </summary>
    public partial class WindowAddSubscription : MetroWindow
    {
        public WindowAddSubscription()
        {
            InitializeComponent();
            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            Win11Style.LoadWin11Style(hWnd);
        }

        public static void Show(MetroWindow owner)
        {
            WindowAddTask dialog = new WindowAddTask();
            dialog.Owner = owner;
            dialog.ShowDialog();
        }

        private async Task<bool> RunUrlDownload(string url)
        {
            var urlResoleResult = await WkyApiManager.Instance.WkyApi.UrlResolve(WkyApiManager.Instance.NowDevice.Peerid, url);
            if (urlResoleResult.Rtn == 0)
            {
                var createResult = await WkyApiManager.Instance.WkyApi.CreateTaskWithUrlResolve(WkyApiManager.Instance.NowDevice.Peerid, WkyApiManager.Instance.GetUsbInfoDefPath(), urlResoleResult);
                if (createResult.Rtn == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmButton.IsEnabled = false;
            //TODO 支持选择设备和磁盘？？
            //WkyAccountManager.WkyApi.CreateTaskWithUrlResolve();

            try
            {
                var result = await RunUrlDownload(UrlTextBox.Text);
                if (result)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("添加异常，请重试", ex.ToString());
            }
            ConfirmButton.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}