using Aria2Fast.Service.Model;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aria2Fast.View
{
    /// <summary>
    /// MikanAnimeRssView.xaml 的交互逻辑
    /// </summary>
    public partial class MikanAnimeRssView : Page
    {
        public MikanAnimeRssView()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var b = sender as Border;

            MikanAnime mikanAnime = ((MikanAnime)DataContext);

            MikanAnimeRss mikanAnimeRss = (MikanAnimeRss)b.DataContext;

            //MainWindow.Instance.RootNavigation.NavigateWithHierarchy(typeof(MikanAnimeRssView), animeItem);
            //弹出订阅页面？
            //MainWindow.Instance.RootNavigation.IsBackButtonVisible = Wpf.Ui.Controls.NavigationViewBackButtonVisible.Visible;
            MainWindow.Instance.RootNavigation.Navigate(typeof(AddSubscriptionView), (mikanAnimeRss.Url, mikanAnime.Name, mikanAnime));
        }
    }
}
