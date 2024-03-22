using Aria2Fast.Service;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aria2Fast.View
{
    /// <summary>
    /// AddTaskView.xaml 的交互逻辑
    /// </summary>
    public partial class AddTaskView : Page
    {
        public AddTaskView()
        {
            InitializeComponent();
            LoadDefaultPathSelected();
        }

        private void LoadDefaultPathSelected()
        {
            try
            {
                var paths = AppConfig.Instance.GetDownloadPathWithAddTask();
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
            try
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
                MaskGrid.Visibility = Visibility.Visible;
                ConfirmButton.IsEnabled = false;
                //TODO 支持选择设备和磁盘？？
                //WkyAccountManager.WkyApi.CreateTaskWithUrlResolve();

                string allLink = UrlTextBox.Text;
                allLink = allLink.Replace("\r\n", "\n");

                var files = allLink.Split("\n");
                files = files.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
                int count = 0;

                foreach (var file in files)
                {
                    if (string.IsNullOrWhiteSpace(file))
                    {
                        continue;
                    }

                    try
                    {
                        var result = await Aria2ApiManager.Instance.DownloadUrl(file, PathComboBox.Text);
                        if (result.isSuccessed)
                        {
                            EasyLogManager.Logger.Info($"任务已添加：{file}");
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine(ex);
                        EasyLogManager.Logger.Error(ex);
                        //await this.ShowMessageAsync("添加异常，请重试", ex.ToString());
                    }
                }


                if (count == 0)
                {
                    EasyLogManager.Logger.Info($"任务添加失败");
                    MainWindow.Instance.ShowSnackbar("失败", "任务添加失败");
                }
                else if (files.Length != count)
                {
                    EasyLogManager.Logger.Info($"成功添加{count}个任务，有{files.Length - count}个添加失败");
                    MainWindow.Instance.ShowSnackbar("成功", $"成功添加{count}个任务，有{files.Length - count}个添加失败");
                    AppConfig.Instance.SaveDownloadPathWithAddTask(PathComboBox.Text);
                }
                else
                {
                    //EasyLogManager.Logger.Info($"成功添加任务");
                    MainWindow.Instance.ShowSnackbar("成功", $"{count}个任务已添加");
                    AppConfig.Instance.SaveDownloadPathWithAddTask(PathComboBox.Text);
                    MainWindow.Instance.RootNavigation.GoBack();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                ConfirmButton.IsEnabled = true;
                MaskGrid.Visibility = Visibility.Collapsed;
            }

            

            
        }




        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.RootNavigation.GoBack();
        }

        private async void UrlTextBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                MaskGrid.Visibility = Visibility.Visible;
                ConfirmButton.IsEnabled = false;

                if (AppConfig.Instance.ConfigData.Aria2UseLocal)
                {
                    //检查本地目录存在
                    if (!PathHelper.LocalPathCheckAndCreate(PathComboBox.Text))
                    {
                        MainWindow.Instance.ShowSnackbar("失败", $"目录 {PathComboBox.Text} 无法使用");
                        return;
                    }
                }

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    int count = 0;
                    foreach (var file in files)
                    {
                        //判断是不是BT
                        if (!file.EndsWith(".torrent"))
                        {
                            EasyLogManager.Logger.Error($"任务不是torrent文件：{file}");
                            continue;
                        }

                        try
                        {
                            var result = await Aria2ApiManager.Instance.DownloadBtFile(file, PathComboBox.Text);
                            if (result.isSuccessed)
                            {
                                EasyLogManager.Logger.Info($"任务已添加：{file}");
                                count++;
                            }

                        }
                        catch (Exception ex)
                        {
                            EasyLogManager.Logger.Error(ex);
                        }

                    }

                    if (count == 0)
                    {
                        EasyLogManager.Logger.Info($"任务添加失败");
                        MainWindow.Instance.ShowSnackbar("失败", "任务添加失败");
                    }
                    else if (files.Length != count)
                    {
                        EasyLogManager.Logger.Info($"成功添加{count}个任务，有{files.Length - count}个添加失败");
                        MainWindow.Instance.ShowSnackbar("成功", $"成功添加{count}个任务，有{files.Length - count}个添加失败");
                        AppConfig.Instance.SaveDownloadPathWithAddTask(PathComboBox.Text);
                    }
                    else
                    {
                        MainWindow.Instance.ShowSnackbar("成功", $"{count}个任务已添加");
                        AppConfig.Instance.SaveDownloadPathWithAddTask(PathComboBox.Text);
                        MainWindow.Instance.RootNavigation.GoBack();
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.Text))
                {
                    //粘贴上去？
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                ConfirmButton.IsEnabled = true;
                MaskGrid.Visibility = Visibility.Collapsed;
            }


            
        }

        private void UrlTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }


        private void TextBoxPath_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}
