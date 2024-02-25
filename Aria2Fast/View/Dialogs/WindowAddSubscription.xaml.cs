
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

        private string _groupNamePath;

        public static void Show(Window owner, string url = "", string title = "")
        {
            WindowAddSubscription dialog = new WindowAddSubscription(url, title);
            dialog.Owner = owner;
            dialog.ShowDialog();
        }


        public WindowAddSubscription(string url, string title)
        {
            InitializeComponent();
            IntPtr hWnd = new WindowInteropHelper(GetWindow(this)).EnsureHandle();
            Win11Style.LoadWin11Style(hWnd);
            LoadDefaultPathSelected();

            _groupNamePath = title;

            UrlTextBox.Text = url;
            TextBoxRssPath.Text = title;


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

        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TextBoxPath.Text.StartsWith("/"))
            {
                MainWindow.Instance.ShowSnackbar("添加失败", $"路径需要用/开头");
                return;
            }

            ConfirmButton.IsEnabled = false;
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
            if (string.IsNullOrEmpty(UrlTextBox.Text) )
            {
                _groupNamePath = string.Empty;
                return;
            }

            try
            {
                CancelButton.IsEnabled = false;
                ConfirmButton.IsEnabled = false;
                var url = UrlTextBox.Text;
                var rssModel = await Task.Run(async () => 
                {
                    var model = SubscriptionManager.Instance.GetSubscriptionInfo(url);
                    return model;
                });

                if (string.IsNullOrEmpty(_groupNamePath)) 
                {
                    TextBoxRssPath.Text = rssModel.SubscriptionName;
                }

                //TODO 
                
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

        private async void TestMatchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestMatchButton.IsEnabled = false;
                RegexCheckBox.IsEnabled = false;
                RegexTextBox.IsEnabled = false;

                var url = UrlTextBox.Text;
                var rssModel = await Task.Run(async () =>
                {
                    var model = SubscriptionManager.Instance.GetSubscriptionInfo(url);
                    return model;
                });

                //根据内容匹配

                foreach (var item in rssModel.SubRssTitles)
                {
                    var titleIsMatch = SubscriptionManager.CheckTitle(RegexTextBox.Text, (bool)RegexCheckBox.IsChecked, item);
                    if (titleIsMatch)
                    {
                        //TODO 加入数组？加入文本
                    }
                }
                
                //TODO弹出提示？

            }
            catch (Exception ex)
            {

            }
            finally
            {
                TestMatchButton.IsEnabled = true;
                RegexCheckBox.IsEnabled = true;
                RegexTextBox.IsEnabled = true;
            }
        }
    }
}
