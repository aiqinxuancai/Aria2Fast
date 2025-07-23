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
using Aria2Fast.Service;
using Aria2Fast.Utils;

using Aria2Fast.Service.Model;

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
            AppConfig.Instance.ConfigData.RemoteAria2Nodes.Add(new Aria2Node() { Name="新增"});
            AppConfig.Instance.Save();
        }

        private void RemoveRemoteAria2_Click(object sender, RoutedEventArgs e)
        {
            if (AppConfig.Instance.ConfigData.CurrentRemoteAria2NodeIndex >= 0 && AppConfig.Instance.ConfigData.CurrentRemoteAria2NodeIndex < AppConfig.Instance.ConfigData.RemoteAria2Nodes.Count)
            {
                AppConfig.Instance.ConfigData.RemoteAria2Nodes.RemoveAt(AppConfig.Instance.ConfigData.CurrentRemoteAria2NodeIndex);
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
