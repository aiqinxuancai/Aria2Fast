﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Aria2Fast.UI.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
      

        public MainWindow()
        {
            DataContext = this;



            //Wpf.Ui.Appearance.Watcher.Watch(this);

            InitializeComponent();

            //Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
            //GAHelper.Instance.Login();

            //_api?.EventReceived
            //    .OfType<DownloadSuccessEvent>()
            //    .Subscribe(async r =>
            //    {
            //        EasyLogManager.Logger.Info($"下载完成 {r.Task.Data.Name} {r.Task.Data.Path}");
            //        if (AppConfig.Instance.ConfigData.PushDeerOpen)
            //        {
            //            await PushDeer.SendPushDeer($"下载完成 {r.Task.Data.Name}", $"用时 {TimeHelper.SecondsToFormatString((int)r.Task.Data.DownTime)}");
            //        }
            //    });


        }

        private ControlAppearance _snackbarAppearance = ControlAppearance.Secondary;


        private void SnackbarBtn_Click(object sender, RoutedEventArgs e)
        {
            //+_snackbar   null    Wpf.Ui.Controls.Snackbar

            //var service = App.GetService<ISnackbarService>();

            SnackbarService snackbarService = new SnackbarService();
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);

            snackbarService.Show(
                "Don't Blame Yourself.",
                "No Witcher's Ever Died In His Bed.",
                _snackbarAppearance,
                new SymbolIcon(SymbolRegular.Fluent24),
                TimeSpan.FromSeconds(5)
            );
        }

        private async void DialogBtn_Click(object sender, RoutedEventArgs e)
        {
            var service = App.GetService<IContentDialogService>();

            service.SetContentPresenter(DialogPresenter);

            var result = await service.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "Save your work?",
                    Content = "aaaa",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                }
            );
        }

    }
}
