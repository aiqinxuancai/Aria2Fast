using Aria2Fast.Dialogs;
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
    /// AnimeListView.xaml 的交互逻辑
    /// </summary>
    public partial class AnimeListView : Page
    {
        public AnimeListView()
        {
            InitializeComponent();

        }

        private void MikanAnimeDayControl_RssBorderClicked(object sender, RssBorderClickedEventArgs e)
        {
            var clickedRssItem = e.ClickedItem;
            var animeItem = e.ParentFeed;


            if (clickedRssItem != null) {
                //TODO 启动一个订阅画面
                WindowAddSubscription.Show(MainWindow.Instance, clickedRssItem.Url, animeItem.Name);
            }
        }
    }
}
