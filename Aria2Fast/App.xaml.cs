using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Aria2Fast.Service;
using Aria2Fast.Utils;
using TiktokenSharp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;
using System.Threading;

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

        public static void ExitAria2Fast()
        {
            if (Aria2Fast.MainWindow.Instance != null)
            {
                Aria2Fast.MainWindow.Instance.Close();
            }

            SubscriptionManager.Instance.Stop();

            Application.Current.Shutdown();

            // 启动一个任务，1 秒后强制退出
            Task.Run(() =>
            {
                Thread.Sleep(1000); // 等待 1 秒
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

            // 其他初始化代码
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 释放互斥体
            _mutex?.ReleaseMutex();
            _mutex?.Close();

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
