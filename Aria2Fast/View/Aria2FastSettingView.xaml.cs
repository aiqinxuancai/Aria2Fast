
using Aria2Fast.Service;
using Aria2Fast.Service.Model;
using Aria2Fast.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aria2Fast.View
{
    /// <summary>
    /// Aria2FastSetting.xaml 的交互逻辑
    /// </summary>
    public partial class Aria2FastSettingView : Page
    {
        public Aria2FastSettingView()
        {
            InitializeComponent();

            //AccountTextBlock.Text = Aria2ApiManager.Instance.API.User;

            //Aria2ApiManager.Instance.API.EventReceived
            //            .OfType<LoginResultEvent>()
            //            .Subscribe(async r =>
            //            {
            //                AccountTextBlock.Text = r.Account;
            //            });

        }

        private void AddRemoteAria2_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.Instance.ConfigData.RemoteAria2Nodes.Add(new Aria2Node());
            AppConfig.Instance.Save();
        }

        private void RemoveRemoteAria2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Aria2Node node)
            {
                AppConfig.Instance.ConfigData.RemoteAria2Nodes.Remove(node);
                AppConfig.Instance.Save();
            }
        }

        private void ApplyRemoteAria2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Aria2Node node)
            {
                if (node != null)
                {
                    if (!Uri.TryCreate(node.URL, new UriCreationOptions(), out Uri fullUri))
                    {
                        MainWindow.Instance.ShowSnackbar("错误", $"无法应用节点 {node.URL}");
                        return;
                    }
                }

                var index = AppConfig.Instance.ConfigData.RemoteAria2Nodes.IndexOf(node);
                if (index != -1)
                {
                    AppConfig.Instance.ConfigData.CurrentRemoteAria2NodeIndex = index;
                }
                AppConfig.Instance.Save();
            }
        }

        private void BadgeNewVersion_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //跳转至最新
            BrowserHelper.OpenUrlBrowser("https://github.com/aiqinxuancai/Aria2Fast/releases/latest");
        }


        private async void LinkAIKEY_Click(object sender, RoutedEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://www.gptapi.us/register?aff=J99N");
        }

        //aihubmix.com
        private async void LinkAIKEYAIHUBMIX_Click(object sender, RoutedEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://aihubmix.com?aff=eeX5");
        }

        private async void LinkAPI2D_Click(object sender, RoutedEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://api2d.com/r/211572");
        }



        private void HomePageTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://github.com/aiqinxuancai/Aria2Fast");
        }
    }
}

