﻿
using Aria2Fast.Service;
using Aria2Fast.Service.Model;
using Aria2Fast.Service.Model.SubscriptionModel;
using Aria2Fast.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
using Wpf.Ui.Controls;

namespace Aria2Fast.View
{
    /// <summary>
    /// AddSubscriptionView.xaml 的交互逻辑
    /// </summary>
    public partial class AddSubscriptionView : Page
    {
        private string _groupNamePath;
        private MikanAnime _anime;

        public AddSubscriptionView()
        {
            InitializeComponent();
            LoadDefaultPathSelected();
            LoadDefaultFilterList();

        }

        private void Page_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var obj = ((ValueTuple<string, string, MikanAnime>)DataContext);

                _groupNamePath = obj.Item2;
                UrlTextBox.Text = obj.Item1;
                TextBoxRssPath.Text = obj.Item2;
                _anime = obj.Item3;
            }
        }

        private void LoadDefaultFilterList()
        {
            SubscriptionFilterItemsControl.ItemsSource = AppConfig.Instance.ConfigData.AddSubscriptionFilterList;

            if (AppConfig.Instance.ConfigData.AddSubscriptionFilterList.Count == 0)
            {
                SubscriptionFilterItemsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                SubscriptionFilterItemsControl.Visibility = Visibility.Visible;
            }
        }

        private void LoadDefaultPathSelected()
        {
            try
            {
                var paths = AppConfig.Instance.GetDownloadPathWithAddSubscription();
                foreach (var item in paths)
                {
                    PathComboBox.Items.Add(item);
                }
                PathComboBox.SelectedIndex = 0;

            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }


        }


        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppConfig.Instance.ConfigData.Aria2UseLocal)
            {
                //检查本地目录存在
                if (!PathHelper.LocalPathCheckAndCreate(PathComboBox.Text))
                {
                    MainWindow.Instance.ShowSnackbar("失败", $"目录 {PathComboBox.Text} 无法使用");
                    return;
                }
            }

            ConfirmButton.IsEnabled = false;
            try
            {
                var list = await GetMatchTitle();

                if (list.Count == 0)
                {
                    //TODO 无法获取订阅；
                    await MainWindow.Instance.ShowMessageBox("错误", "无法获取订阅内容", null, null, null, null, "确定");
                    return;
                }

                bool isContinue = false;
                await MainWindow.Instance.ShowMessageBox("将会订阅以下内容，请确认", string.Join("\n", list),  () => { isContinue = true; }, null, "确定", null, "取消");

                if (!isContinue)
                {
                    return;
                }
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
                        path = PathComboBox.Text;
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
                            path = PathComboBox.Text;
                        }
                        else
                        {
                            path = PathComboBox.Text + (PathComboBox.Text.EndsWith("/") ? "" : "/") + title;
                        }

                        SubscriptionManager.Instance.Add(url, path, regex, regexEnable, autoDir: autoDir);
                        EasyLogManager.Logger.Info($"订阅已添加：{title} {url}");

                        MainWindow.Instance.ShowSnackbar("添加成功", $"已添加订阅{title}", SymbolRegular.AddCircle24);
                        AppConfig.Instance.SaveDownloadPathWithAddSubscription(PathComboBox.Text);
                        if (!string.IsNullOrWhiteSpace(RegexTextBox.Text))
                        {
                            var list = AppConfig.Instance.ConfigData.AddSubscriptionFilterList;
                            if (list.Count > 10)
                            {
                                list.RemoveAt(list.Count - 1);
                            }
                            AppConfig.Instance.ConfigData.AddSubscriptionFilterList.Add(
                                new SubscriptionFilterModel()
                                {
                                    Filter = RegexTextBox.Text,
                                    IsFilterRegex = (bool)RegexCheckBox.IsChecked
                                }

                            );
                        }
                        
                    });

                });

                //TODO this.Close();
                Back();



            }
            catch (Exception ex)
            {
                //await this.ShowMessageAsync("添加异常，请重试", ex.ToString());
                EasyLogManager.Logger.Error(ex);
            } 
            finally
            {
                ConfirmButton.IsEnabled = true;
            }
            
        }

        private void Back()
        {
            if (_anime != null)
            {
                MainWindow.Instance.RootNavigation.Navigate(typeof(MikanAnimeRssView), _anime);
            }
            else
            {
                MainWindow.Instance.RootNavigation.GoBack();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Back();
        }


        private void TextBoxPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            //当前选择的设备ID
            //AppConfig.Instance.ConfigData.AddSubscriptionSavePathDict[AppConfig.Instance.ConfigData.Aria2RpcAuto] = TextBoxPath.Text;
            //AppConfig.Instance.Save();
        }

        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(UrlTextBox.Text))
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

                var list = await GetMatchTitle();


                MainWindow.Instance.ShowMessageBox($"匹配结果[{list.Count}]",string.Join("\n", list), null, null, null, null);

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

        private async Task<List<string>> GetMatchTitle()
        {
            var url = UrlTextBox.Text;
            var rssModel = await Task.Run(async () =>
            {
                var model = SubscriptionManager.Instance.GetSubscriptionInfo(url);
                return model;
            });

            if (rssModel == null)
            {
                return new List<string>();
            }

            //根据内容匹配
            List<string> list = new List<string>();
            foreach (var item in rssModel.SubRssTitles)
            {
                var titleIsMatch = SubscriptionManager.CheckTitle(RegexTextBox.Text, (bool)RegexCheckBox.IsChecked, item);
                if (titleIsMatch)
                {
                    //TODO 加入数组？加入文本
                    list.Add(item);
                }
            }
            return list;
        }


        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var obj = sender as Border;
            if (obj != null)
            {
                var model = obj.DataContext as SubscriptionFilterModel;
                this.RegexTextBox.Text = model.Filter;
                this.RegexCheckBox.IsChecked = model.IsFilterRegex;
            }
        }
    }
}
