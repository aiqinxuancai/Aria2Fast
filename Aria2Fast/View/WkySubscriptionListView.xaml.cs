using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aria2Fast.Service;
using Aria2Fast.Service.Model;
using Aria2Fast.Service.Model.SubscriptionModel;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json;



namespace Aria2Fast.View
{
    /// <summary>
    /// WkyTaskListView.xaml 的交互逻辑
    /// </summary>
    public partial class WkySubscriptionListView : Page
    {
        public WkySubscriptionListView()
                    : this(new ObservableCollection<SubscriptionModel>())
        { }

        public WkySubscriptionListView(ObservableCollection<SubscriptionModel> viewModel)
        {
            InitializeComponent();

            //主动刷新？
            this.ViewModel = viewModel;
            this.ViewModel = SubscriptionManager.Instance.SubscriptionModel; //订阅列表绑定
            this.SubscriptionButton.IsEnabled = Aria2ApiManager.Instance.Connected;

            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginResultEvent>()
                .SubscribeOnMainThread(async r =>
                {
                    if (r.IsSuccess)
                    {
                        this.SubscriptionButton.IsEnabled = true;
                    }
                    else
                    {
                        this.SubscriptionButton.IsEnabled = false;
                    }

                });
        }

        List<SubscriptionModel> _selectedItems;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ObservableCollection<SubscriptionModel>), typeof(WkySubscriptionListView));

        public ObservableCollection<SubscriptionModel> ViewModel
        {
            get { return (ObservableCollection<SubscriptionModel>)GetValue(ViewModelProperty); }
            set
            {
                SetValue(ViewModelProperty, value);
                if (value != null && value.Count > 0)
                {
                    MainDataGrid.SelectedItem = value.First();
                }
            }
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        private void MainDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //展开时，需要暂停刷新？
            var contextMenu = MainDataGrid.ContextMenu;
            contextMenu.Items.Clear();

            var selectedItems = new List<SubscriptionModel>();
            foreach (var item in MainDataGrid.SelectedItems)
            {
                var myItem = item as SubscriptionModel;
                if (myItem != null)
                {
                    selectedItems.Add(myItem);
                }
            }

            var jsonString = JsonConvert.SerializeObject(selectedItems);

            //需要深拷贝
            _selectedItems = JsonConvert.DeserializeObject<List<SubscriptionModel>>(jsonString);

            if (_selectedItems.Count > 0)
            {
                if (AppConfig.Instance.ConfigData.Aria2UseLocal && _selectedItems.Count == 1)
                {
                    MenuItem menuOpenPath = new MenuItem() { Header = "打开目录" };
                    menuOpenPath.Click += MenuOpenPath_Click;
                    contextMenu.Items.Add(menuOpenPath);
                }

                if (_selectedItems.Count == 1)
                {
                    MenuItem menuEdit = new MenuItem() { Header = "编辑" };
                    menuEdit.Click += MenuEdit_Click;
                    contextMenu.Items.Add(menuEdit);
                }


                MenuItem menuCopyUrl = new MenuItem() { Header = "复制URL" };
                menuCopyUrl.Click += MenuCopyUrl_Click; ;

                MenuItem menuReDownload = new MenuItem() { Header = "重新下载" };
                menuReDownload.Click += MenuReDownload_Click; ;

                MenuItem menuDelete = new MenuItem() { Header = "删除" };
                menuDelete.Click += MenuDelete_Click;

                contextMenu.Items.Add(menuCopyUrl);
                contextMenu.Items.Add(menuReDownload);
                contextMenu.Items.Add(menuDelete);
                DataGrid row = sender as DataGrid;
                row.ContextMenu = contextMenu;

            }

            


        }


        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            var model = _selectedItems.FirstOrDefault();

            //编辑
            MainWindow.Instance.RootNavigation.Navigate(typeof(AddSubscriptionView), (model.Url, model.Name, new MikanAnime(), model));

        }

        private void MenuOpenPath_Click(object sender, RoutedEventArgs e)
        {
            var model = _selectedItems.FirstOrDefault();

            if (Directory.Exists(model.SavePath))
            {
                string correctedPath = System.IO.Path.GetFullPath(model.SavePath);
                Process.Start("explorer.exe", correctedPath);
            } 
        }
        
        private void MenuCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            var urls = _selectedItems.Select(item => item.Url);
            Clipboard.SetDataObject(string.Join("\r\n", urls));
        }

        private void MenuReDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!SubscriptionManager.Instance.Subscribing)
            {
                Task.Run(async () =>
                {
                    foreach (var item in _selectedItems)
                    {
                        item.AlreadyAddedDownloadModel = new ObservableCollection<SubscriptionSubTaskModel> { };
                        await SubscriptionManager.Instance.CheckSubscriptionOne(item, Aria2ApiManager.Instance.CurrentRpc);
                    }
                });
            }
            else
            {
                MainWindow.Instance.ShowSnackbar("无法操作", $"正在执行订阅中");
            }

        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _selectedItems)
            {
                SubscriptionManager.Instance.SubscriptionModel.Remove(item);
            }
            SubscriptionManager.Instance.Save();
        }

        private void SubscriptionButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.RootNavigation.Navigate(typeof(AddSubscriptionView), null);

        }

        private void MainDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

        }
    }
}
