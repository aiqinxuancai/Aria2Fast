
using Aria2Fast.Service;
using Aria2Fast.Service.Model;
using Aria2Fast.Services;
using Aria2Fast.Utils;
using Aria2Fast.View;
using Aria2Fast.View.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Aria2Fast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public static MainWindow Instance { get; set; }

        private CancellationTokenSource _tokenTaskListSource = new CancellationTokenSource();

        private bool _needExit = false;

        private const string TaskbarCreatedMessageName = "TaskbarCreated";
        private uint _taskbarCreatedMessage;
        private HwndSource? _mainHwndSource;
        private HwndSourceHook? _mainHwndSourceHook;

        public IList<object> NavigationItems { set; get; } = new ObservableCollection<object>()
        {
            new NavigationViewItem("任务", SymbolRegular.TextBulletListSquare20, typeof(WkyTaskListView))
            {
                NavigationCacheMode = NavigationCacheMode.Required,
            },
            new NavigationViewItem("我的订阅", SymbolRegular.AppFolder24, typeof(WkySubscriptionListView))
            {
                NavigationCacheMode = NavigationCacheMode.Required,
            },
            new NavigationViewItem("订阅源", SymbolRegular.StarAdd24, null)
            {
                NavigationCacheMode = NavigationCacheMode.Required,
                MenuItemsSource = new object[]
                {
                    new NavigationViewItem("Mikan", typeof(AnimeListView))
                    {
                        NavigationCacheMode = NavigationCacheMode.Required,
                    },
                },
            },
        };


        public MainWindow()
        {
            DataContext = this;

            InitializeComponent();
            ThemeManager.ApplyTheme(AppConfig.Instance.ConfigData.AppTheme, this);
            //ApplicationThemeManager.Apply(ApplicationTheme.Light);
           
            Instance = this;

            SourceInitialized += MainWindow_SourceInitialized;
            Closed += MainWindow_Closed;

            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            Win11Style.LoadWin11Style(hWnd);
            EasyLogManager.Logger.Info("主界面初始化");

        }

        ~MainWindow()
        {
            EasyLogManager.Logger.Info("主界面销毁");
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitNavigationViewItem();


            EnsureNotifyIconRegistered("MetroWindow_Loaded");

           

            RootNavigation.Navigate(typeof(WkyTaskListView));

            ActionVersion.CheckVersion();
            SubscriptionManager.Instance.OnSubscriptionProgressChanged += SubscriptionManager_OnSubscriptionProgressChanged;
            MikanManager.Instance.EventReceived
                .OfType<MikanListProgressEvent>()
                .SubscribeOnMainThread(async r =>
                {
                    UpdateMikanListProgress(r);
                });

            MikanManager.Instance.EventReceived
                .OfType<MikanAiProgressEvent>()
                .SubscribeOnMainThread(async r =>
                {
                    UpdateAiProgress(r);
                });



            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginStartEvent>()
                .SubscribeOnMainThread(async r =>
                {
                    UpdateConnectionStatus(LinkStatus.Linking);
                });


            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginResultEvent>()
                .SubscribeOnMainThread(async r =>
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

            Aria2ApiManager.Instance.Init();
            MikanManager.Instance.MikanStart(false);
            GameAnalyticsManager.Instance.InitializeAsync(AppConfig.Instance.ConfigData.ClientId);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string lpString);

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                _mainHwndSource = HwndSource.FromHwnd(hwnd);
                if (_mainHwndSource is null)
                {
                    return;
                }

                _taskbarCreatedMessage = RegisterWindowMessage(TaskbarCreatedMessageName);
                _mainHwndSourceHook ??= WndProc;
                _mainHwndSource.AddHook(_mainHwndSourceHook);

                EnsureNotifyIconRegistered("SourceInitialized");
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                if (_mainHwndSource is not null && _mainHwndSourceHook is not null)
                {
                    _mainHwndSource.RemoveHook(_mainHwndSourceHook);
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_taskbarCreatedMessage != 0 && msg == unchecked((int)_taskbarCreatedMessage))
            {
                // Explorer / 任务栏重启后系统会广播 TaskbarCreated，需重新注册托盘图标
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => RecreateNotifyIcon("TaskbarCreated")));
            }

            return IntPtr.Zero;
        }

        private void EnsureNotifyIconRegistered(string reason)
        {
            try
            {
                if (MyNotifyIcon is null)
                {
                    return;
                }

                var hwnd = new WindowInteropHelper(this).Handle;
                _mainHwndSource ??= HwndSource.FromHwnd(hwnd);

                if (_mainHwndSource is not null)
                {
                    MyNotifyIcon.HookWindow = _mainHwndSource;
                }

                MyNotifyIcon.ParentHandle = hwnd;

                if (!MyNotifyIcon.IsRegistered)
                {
                    MyNotifyIcon.Register();
                    EasyLogManager.Logger.Info($"托盘图标已注册 ({reason})");
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private void RecreateNotifyIcon(string reason)
        {
            try
            {
                if (MyNotifyIcon is null)
                {
                    return;
                }

                var hwnd = new WindowInteropHelper(this).Handle;
                _mainHwndSource ??= HwndSource.FromHwnd(hwnd);

                if (_mainHwndSource is not null)
                {
                    MyNotifyIcon.HookWindow = _mainHwndSource;
                }

                MyNotifyIcon.ParentHandle = hwnd;

                try
                {
                    MyNotifyIcon.Unregister();
                }
                catch
                {
                    // ignore - best effort
                }

                MyNotifyIcon.Register();
                EasyLogManager.Logger.Info($"托盘图标已重建 ({reason})");
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private void InitNavigationViewItem()
        {
            foreach (NavigationViewItem item in NavigationItems)
            {
                var mainBorder = item.Template.FindName("MainBorder", item) as Border;
                if (mainBorder != null)
                {
                    mainBorder.MinWidth = 200; // 或您想要的任意数值
                }
            }
            var mb = SettingNavigationItem.Template.FindName("MainBorder", SettingNavigationItem) as Border;
            if (mb != null)
            {
                mb.MinWidth = 150; // 或您想要的任意数值
            }
        }

        private void SubscriptionManager_OnSubscriptionProgressChanged(int now, int max, string currentRssName)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (now == 0 && max > 0)
                {
                    RssStatusBorder.Visibility = Visibility.Visible;
                }
                else if (now == max && max > 0 && string.IsNullOrWhiteSpace(currentRssName))
                {
                    RssStatusBorder.Visibility = Visibility.Collapsed;
                }
                else if (max == 0)
                {
                    RssStatusBorder.Visibility = Visibility.Collapsed;
                }

                RssStatusTextBlock.Text = @$"({now + 1}/{max}) {currentRssName}";

            }));
        }

        private void UpdateMikanListProgress(MikanListProgressEvent progress)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (progress.Total <= 0)
                {
                    MikanListStatusBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                if (progress.Current >= progress.Total && string.IsNullOrWhiteSpace(progress.Name))
                {
                    MikanListStatusBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                MikanListStatusBorder.Visibility = Visibility.Visible;

                var current = Math.Min(progress.Current, progress.Total);
                MikanListStatusTextBlock.Text = $"列表 ({current}/{progress.Total}) {progress.Name}";
            }));
        }

        private void UpdateAiProgress(MikanAiProgressEvent progress)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (progress.Total <= 0)
                {
                    AiStatusBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                if (progress.Current >= progress.Total && string.IsNullOrWhiteSpace(progress.Name))
                {
                    AiStatusBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                AiStatusBorder.Visibility = Visibility.Visible;

                var current = Math.Min(progress.Current, progress.Total);
                AiStatusTextBlock.Text = $"AI ({current}/{progress.Total}) {progress.Name}";
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
            this.Hide();
            // this.Close();
            //TODO 退出进程
           
            Task.Run(async () =>
            {
                await App.ExitAria2Fast();
            });
           
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_needExit)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        //更新连接状态
        private void UpdateConnectionStatus(LinkStatus status)
        {
            //连接失败
            //连接中
            //连接成功

            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (status)
                {
                    case LinkStatus.Linking:
                        //LinkStatusProgressBar.IsIndeterminate = true;
                        LinkStatusProgressRing.Visibility = Visibility.Visible;
                        LinkStatusBorder.SetResourceReference(Border.BackgroundProperty, "App.StatusLinkingBrush");
                        break;
                    case LinkStatus.Error:
                        //LinkStatusProgressBar.IsIndeterminate = false;
                        LinkStatusProgressRing.Visibility = Visibility.Collapsed;
                        LinkStatusBorder.SetResourceReference(Border.BackgroundProperty, "App.StatusErrorBrush");
                        break;
                    case LinkStatus.Success:
                        //LinkStatusProgressBar.IsIndeterminate = false;
                        LinkStatusProgressRing.Visibility = Visibility.Collapsed;
                        LinkStatusBorder.SetResourceReference(Border.BackgroundProperty, "App.StatusSuccessBrush");
                        break;

                }
            });

            
        }


        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MyNotifyIcon_LeftClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }
    }
}
