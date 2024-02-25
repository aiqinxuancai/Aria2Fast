using Aria2Fast.Service.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class RssBorderClickedEventArgs : RoutedEventArgs
    {
        public MikanAnimeRss ClickedItem { get; private set; }
        public MikanAnime ParentFeed { get; private set; }

        public RssBorderClickedEventArgs(RoutedEvent routedEvent, object source, MikanAnimeRss clickedItem, MikanAnime parentFeed)
            : base(routedEvent, source)
        {
            ClickedItem = clickedItem;
            ParentFeed = parentFeed;
        }
    }
    /// <summary>
    /// MikanAnimeDayControl.xaml 的交互逻辑
    /// </summary>
    public partial class MikanAnimeDayControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
        public double ListViewWidth { get; set; }

        public static readonly RoutedEvent RssBorderClickedEvent = EventManager.RegisterRoutedEvent(
        "RssBorderClicked",
        RoutingStrategy.Bubble,
        typeof(EventHandler<RssBorderClickedEventArgs>),
        typeof(MikanAnimeDayControl));

        public event EventHandler<RssBorderClickedEventArgs> RssBorderClicked
        {
            add { AddHandler(RssBorderClickedEvent, value); }
            remove { RemoveHandler(RssBorderClickedEvent, value); }
        }

        protected virtual void OnRssBorderClicked(MikanAnimeRss item, MikanAnime parentFeed)
        {
            RssBorderClickedEventArgs args = new RssBorderClickedEventArgs(
                RssBorderClickedEvent, this, item, parentFeed);
            RaiseEvent(args);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border rssBorder = sender as Border;
            if (rssBorder == null) return;

            var listViewItem = FindAncestor<ListViewItem>(rssBorder);
            if (listViewItem == null) return;

            var rssFeed = listViewItem.DataContext as MikanAnime;
            if (rssFeed == null) return;

            OnRssBorderClicked(rssBorder.DataContext as MikanAnimeRss, rssFeed);
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }



        public MikanAnimeDayControl()
        {
            //DataContext = this;
            InitializeComponent();
        }

        

    }
}
