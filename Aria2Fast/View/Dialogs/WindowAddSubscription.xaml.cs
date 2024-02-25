
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Aria2Fast.Service;
using Aria2Fast.Utils;
using Wpf.Ui.Controls;

namespace Aria2Fast.Dialogs
{
    /// <summary>
    /// WindowAddTask.xaml 的交互逻辑
    /// </summary>
    public partial class WindowAddSubscription : FluentWindow
    {

        public WindowAddSubscription()
        {
            InitializeComponent();
            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            Win11Style.LoadWin11Style(hWnd);
            LoadDefaultPathSelected();
        }

        public static void Show(Window owner)
        {
            WindowAddSubscription dialog = new WindowAddSubscription();
            dialog.Owner = owner;
            dialog.ShowDialog();
        }

        private void LoadDefaultPathSelected()
        {
            try
            {
                if (AppConfig.Instance.ConfigData.AddTaskSavePathDict.TryGetValue(AppConfig.Instance.ConfigData.Aria2Rpc, out var path))
                {
                    this.TextBoxPath.Text = path;
                }
                else
                {
                    this.TextBoxPath.Text = "/downloads";
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }

        }


        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //if (string.IsNullOrWhiteSpace(AppConfig.Instance.ConfigData.LastAddSubscriptionPath))
            //{
            //    TextBoxPath.Text = "/onecloud/tddownload";
            //}
            //else
            //{
            //    //TODO
            //    TextBoxPath.Text = AppConfig.Instance.ConfigData.LastAddSubscriptionPath;
            //}

        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TextBoxPath.Text.StartsWith("/"))
            {
                MainWindow.Instance.ShowSnackbar("添加失败", $"路径需要用/开头");
                return;
            }

            ConfirmButton.IsEnabled = false;
            //TODO 支持选择设备和磁盘？？
            try
            {
                await Task.Run(() => {
                    string url = string.Empty;
                    string regex = string.Empty;
                    bool regexEnable = false;
                    string path = string.Empty;
                    bool autoDir = false;

                    this.Dispatcher.Invoke(() =>
                    {
                        url = UrlTextBox.Text;
                        regex = RegexTextBox.Text;
                        regexEnable = RegexCheckBox.IsChecked == true ? true : false;
                        path = TextBoxPath.Text;
                        autoDir = AutoDirSwitch.IsChecked == true ? true : false;
                    });

                    try
                    {
                        Uri uri = new Uri(url);
                    } 
                    catch (Exception ex)
                    {
                        //progressView.CloseAsync();
                        //this.ShowMessageAsync("Url不合法", ex.ToString());
                        //return;
                        EasyLogManager.Logger.Error(ex);
                    }

                   

                    this.Dispatcher.Invoke(() =>
                    {
                        string title = TextBoxRssPath.Text;

                        if (string.IsNullOrWhiteSpace(title))
                        {
                            path = TextBoxPath.Text;
                        }
                        else
                        {
                            path = TextBoxPath.Text + (TextBoxPath.Text.EndsWith("/") ? "" : "/") + title;
                        }

                        SubscriptionManager.Instance.Add(url, path, regex, regexEnable, autoDir: autoDir);
                        EasyLogManager.Logger.Info($"订阅已添加：{title} {url}");

                        MainWindow.Instance.ShowSnackbar("添加成功", $"已添加订阅{title}", SymbolRegular.AddCircle24);
                    });

                });

                //await progressView.CloseAsync();

                this.Close();
            }
            catch (Exception ex)
            {
                //await this.ShowMessageAsync("添加异常，请重试", ex.ToString());
                EasyLogManager.Logger.Error(ex);
            }
            ConfirmButton.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void TextBoxPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            //当前选择的设备ID
            AppConfig.Instance.ConfigData.AddSubscriptionSavePathDict[AppConfig.Instance.ConfigData.Aria2Rpc] = TextBoxPath.Text;
            AppConfig.Instance.Save();
        }

        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CancelButton.IsEnabled = false;
                ConfirmButton.IsEnabled = false;
                var url = UrlTextBox.Text;
                string title = await Task.Run(async () => 
                {
                    return SubscriptionManager.Instance.GetSubscriptionTitle(url);
                });

                TextBoxRssPath.Text = title;
            }
            catch (Exception ex)
            {

            }
            finally
            {
                CancelButton.IsEnabled = true;
                ConfirmButton.IsEnabled = true;
            }

        }
    }
}
