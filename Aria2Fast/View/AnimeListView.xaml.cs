using Aria2Fast.Dialogs;
using Aria2Fast.Service;
using Aria2Fast.Service.Model;
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
    /// AnimeListView.xaml 的交互逻辑
    /// </summary>
    public partial class AnimeListView : Page
    {
        public AnimeListView()
        {
            InitializeComponent();

            GetListButton.IsEnabled = !MikanManager.Instance.IsLoading;

            //IsLoading
            MikanManager.Instance.EventReceived
                .OfType<MikanListLoaded>()
                .Subscribe(async r =>
                {
                    //刷新订阅？
                    this.DataContext = MikanManager.Instance;
                    GetListButton.IsEnabled = true;
                });
        }

        private async void MikanAnimeDayControl_RssBorderClicked(object sender, RssBorderClickedEventArgs e)
        {
            var clickedRssItem = e.ClickedItem;
            var animeItem = e.ParentFeed;


            if (clickedRssItem != null) {
                //启动一个订阅画面
                //WindowAddSubscription.Show(MainWindow.Instance, clickedRssItem.Url, animeItem.Name);

                var data = (clickedRssItem.Url, animeItem.Name);
                MainWindow.Instance.RootNavigation.Navigate(typeof(AddSubscriptionView), data);
            } 
            else if (animeItem != null)
            {
                //await Task.Delay(50);
                //TestN.IsOpen = true;
            }
            
        }

        private async void GetListButton_Click(object sender, RoutedEventArgs e)
        {
            GetListButton.IsEnabled = false;

            await MikanManager.Instance.MikanStart(true);

        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //SearchTextBox
            MikanManager.Instance.SearchText = SearchTextBox.Text;
            this.DataContext = null;
            this.DataContext = MikanManager.Instance;
        }
    }
}
