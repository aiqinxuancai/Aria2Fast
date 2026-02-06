using Aria2Fast.Service;
using Aria2Fast.Services;
using Aria2Fast.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui;

namespace Aria2Fast
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "Aria2Fast_Process_Mutex";

        private Mutex _mutex;


        private static readonly IHost _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                c.SetBasePath(AppContext.BaseDirectory);
            })
            .ConfigureServices(
                (context, services) =>
                {
                    // App Host
                    //services.AddHostedService<ApplicationHostService>();

                    // Main window container with navigation
                    // services.AddSingleton<IWindow, MainWindow>();
                    // services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<ISnackbarService, SnackbarService>();
                    services.AddSingleton<IContentDialogService, ContentDialogService>();
                    //services.AddSingleton<WindowsProviderService>();
                }
            )
            .Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T? GetService<T>() where T : class
        {
            return _host.Services.GetService(typeof(T)) as T ?? null;
        }


        static App()
        {
            var a = MikanManager.Instance;
            TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(Window),
                new FrameworkPropertyMetadata(TextFormattingMode.Display, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
        }

        public static async Task ExitAria2Fast()
        {
            Debug.WriteLine("开始退出");
            await GameAnalyticsManager.Instance.ShutdownAsync();

            if (Aria2Fast.MainWindow.Instance != null)
            {
                Debug.WriteLine("销毁主窗口");
                try
                {
                    Aria2Fast.MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
                        Aria2Fast.MainWindow.Instance.Close();
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"销毁主窗口错误{ex}");
                }
                
            }
            Debug.WriteLine("开始停止订阅器#2");
            SubscriptionManager.Instance.Stop();

            // 启动一个任务，1 秒后强制退出
            await Task.Run(() =>
            {
                Thread.Sleep(2000); // 等待 1 秒
                Debug.WriteLine("强制退出");
                Environment.Exit(0); // 强制退出
                
            });

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isNewInstance;
            _mutex = new Mutex(true, MutexName, out isNewInstance);

            if (!isNewInstance)
            {
                // 弹出一个消息框询问用户是否继续执行
                MessageBoxResult result = MessageBox.Show(
                    "Aria2Fast已经在运行中，是否继续启动新的进程？",
                    "提示",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    // 用户选择“否”，退出应用程序
                    ExitAria2Fast();
                    return;
                }
                // 用户选择“是”，继续执行
            }

            // 继续启动应用程序
            base.OnStartup(e);

            ThemeManager.ApplyTheme(AppConfig.Instance.ConfigData.AppTheme);

            // 其他初始化代码
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 释放互斥体
                _mutex?.ReleaseMutex();
                _mutex?.Close();

            } catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
                //MessageBox.Show(ex?.Message + Environment.NewLine + ex?.InnerException?.ToString(), "Error#3", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            base.OnExit(e);
        }


        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            EasyLogManager.Logger.Error(ex);
            MessageBox.Show(ex?.Message + Environment.NewLine + ex?.InnerException?.ToString(), "Error#1", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            EasyLogManager.Logger.Error(e.Exception);
            MessageBox.Show(e?.Exception?.Message + Environment.NewLine + e?.Exception?.InnerException?.ToString(), "Error#2", MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
        }
    }
}
