
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aria2Fast.Service;
using Aria2Fast.Utils;
using Aria2Fast.View.Model;
using Aria2Fast.Dialogs;
using System.Threading;
using Wpf.Ui.Controls;
using System.Reactive.Linq;
using Aria2Fast.Service.Model;
using Wpf.Ui.Appearance;
using Wpf.Ui;
using Aria2Fast.View;

namespace Aria2Fast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow, IContentDialogService
    {
        public static MainWindow Instance { get; set; }

        private CancellationTokenSource _tokenTaskListSource = new CancellationTokenSource();

        private bool _needExit = false;

        public IList<object> NavigationItems { set; get; } = new ObservableCollection<object>()
        {
            new NavigationViewItem("任务", SymbolRegular.TextBulletListSquare20, typeof(WkyTaskListView))
            {
            },
            new NavigationViewItem("订阅", SymbolRegular.AppFolder24, typeof(WkySubscriptionListView))
            {
                MenuItems = new object[]
                {
                    new NavigationViewItem("Mikan", typeof(AnimeListView)),
                },
            },
        };


        public MainWindow()
        {
            DataContext = this;





            InitializeComponent();


            

            SystemThemeWatcher.Watch(this);

            Instance = this;
            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            Win11Style.LoadWin11Style(hWnd);
            EasyLogManager.Logger.Info("主界面初始化");

        }

        ~MainWindow()
        {

        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItem item in NavigationItems)
            {

                var mainBorder = item.Template.FindName("MainBorder", item) as Border;
                if (mainBorder != null)
                {
                    mainBorder.MinWidth = 200; // 或您想要的任意数值
                }

            }

            ApplicationThemeManager.Apply(ApplicationTheme.Light);

            //RootNavigation.MenuItemsSource = _menuItems;

            RootNavigation.Navigate(typeof(WkyTaskListView));

            ActionVersion.CheckVersion();
            SubscriptionManager.Instance.OnSubscriptionProgressChanged += SubscriptionManager_OnSubscriptionProgressChanged;
            GAHelper.Instance.RequestPageView($"启动到主界面{ActionVersion.Version}");


            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginStartEvent>()
                .Subscribe(async r =>
                {
                    UpdateConnectionStatus(LinkStatus.Linking);
                });


            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginResultEvent>()
                .Subscribe(async r =>
                {
                    if (r.IsSuccess)
                    {
                        UpdateConnectionStatus(LinkStatus.Success);
                    }
                    else
                    {
                        UpdateConnectionStatus(LinkStatus.Error);
                    }
                });

            //开始连接
            Aria2ApiManager.Instance.Init();

            MikanManager.Instance.MikanStart(false);



        }

        private void SubscriptionManager_OnSubscriptionProgressChanged(int now, int max)
        {

            this.Dispatcher.Invoke(new Action(() =>
            {
                if (max > 0 && now != max)
                {
                    //进程中
                    this.subscriptionProgressBar.Visibility = Visibility.Visible;
                    this.subscriptionProgressBar.Value = now;
                    this.subscriptionProgressBar.Maximum = max;
                }
                else
                {
                    this.subscriptionProgressBar.Visibility = Visibility.Hidden;
                    this.subscriptionProgressBar.Value = 0;
                    this.subscriptionProgressBar.Maximum = 1;
                }


            }));


        }

        private void MetroWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            //退出
            SubscriptionManager.Instance.Stop();
            _tokenTaskListSource.Cancel();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private void ButtonGithub_Click(object sender, RoutedEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://github.com/aiqinxuancai/Aria2Fast");
        }

        private void MainNotifyIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Hidden)
            {
                this.Visibility = Visibility.Visible;
                this.Focus();
            }
            else
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        private void NavigationItem_Home_Click(object sender, RoutedEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://github.com/aiqinxuancai/Aria2Fast");
        }

        private void HomeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BrowserHelper.OpenUrlBrowser("https://github.com/aiqinxuancai/Aria2Fast");
        }

        private void TitleBar_CloseClicked(object sender, RoutedEventArgs e)
        {
            //自行处理事件，改为最小化
        }

        private void TaskbarExitMenu_Click(object sender, RoutedEventArgs e)
        {
            _needExit = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_needExit)
            {
                e.Cancel = true;

                // 自己处理
                this.Hide();
                //弹出提示
                //MyNotifyIcon.
            }
        }

        //更新连接状态
        private void UpdateConnectionStatus(LinkStatus status)
        {
            //连接失败
            //连接中
            //连接成功

            SolidColorBrush myBrush = new SolidColorBrush();

            switch (status)
            {
                case LinkStatus.Linking:
                    LinkStatusProgressBar.IsIndeterminate = true;
                    //LinkStatusProgressBar.Visibility = Visibility.Visible;
                    myBrush.Color = (Color)ColorConverter.ConvertFromString("#2db7f5");
                    LinkStatusBorder.Background = myBrush;
                    break;
                case LinkStatus.Error:
                    LinkStatusProgressBar.IsIndeterminate = false;
                    //LinkStatusProgressBar.Visibility = Visibility.Collapsed;
                    myBrush.Color = (Color)ColorConverter.ConvertFromString("#ffed4014");
                    LinkStatusBorder.Background = myBrush;
                    break;
                case LinkStatus.Success:
                    LinkStatusProgressBar.IsIndeterminate = false;
                    //LinkStatusProgressBar.Visibility = Visibility.Collapsed;
                    myBrush.Color = (Color)ColorConverter.ConvertFromString("#ff19be6b");
                    LinkStatusBorder.Background = myBrush;
                    

                    break;

            }
            
        }

        public INavigationView GetNavigation()
        {
            throw new NotImplementedException();
        }

        public bool Navigate(Type pageType)
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        public void SetPageService(IPageService pageService)
        {
            throw new NotImplementedException();
        }

        public void ShowWindow()
        {
            throw new NotImplementedException();
        }

        public void CloseWindow()
        {
            throw new NotImplementedException();
        }

        public void SetContentPresenter(ContentPresenter contentPresenter)
        {
            throw new NotImplementedException();
        }

        public ContentPresenter GetContentPresenter()
        {
            throw new NotImplementedException();
        }

        public Task<ContentDialogResult> ShowAsync(ContentDialog dialog, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
