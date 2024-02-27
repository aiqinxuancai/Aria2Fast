using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;


namespace Aria2Fast
{
    public partial class MainWindow
    {
        //注册事件，用于弹出提示等操作

        private Action _messageBoxLeft;
        private Action _messageBoxRight;


        public async Task ShowMessageBox(string title, string message, Action leftClick, Action rightClick, string buttonLeftName = "Yes", string buttonRightName = "No", string buttonCancelName = "Cancel")
        {
            var service = App.GetService<IContentDialogService>();

            service.SetContentPresenter(DialogPresenter);


            var opt = new SimpleContentDialogCreateOptions()
            {
                Title = title,
                Content = message,
                CloseButtonText = buttonCancelName 
            };

            if (!string.IsNullOrWhiteSpace(buttonLeftName))
            {
                opt.PrimaryButtonText = buttonLeftName;
            }
            if (!string.IsNullOrWhiteSpace(buttonRightName))
            {
                opt.SecondaryButtonText = buttonRightName;
            }

            var result = await service.ShowSimpleDialogAsync(
                opt
            );

            Action call = result switch
            {
                ContentDialogResult.Primary => leftClick,
                ContentDialogResult.Secondary => rightClick,
                _ => () => { }
            };

            call();
        }

        /// <summary>
        /// 展示提示类
        /// </summary>
        public void ShowSnackbar(string title, string message, SymbolRegular icon = SymbolRegular.Info24)
        {
            SnackbarService snackbarService = new SnackbarService();
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);

            snackbarService.Show(
                title,
                message,
                ControlAppearance.Secondary,
                new SymbolIcon(icon),
                TimeSpan.FromSeconds(5)
            );
        }

    }
}
