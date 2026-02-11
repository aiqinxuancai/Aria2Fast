using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
using Aria2Fast.Service;
using Aria2Fast.Service.Model;
using Newtonsoft.Json;



namespace Aria2Fast.View
{
    /// <summary>
    /// WkyTaskListView.xaml 的交互逻辑
    /// </summary>
    public partial class WkyTaskListView : Page
    {
        public WkyTaskListView()
                    : this(new ObservableCollection<TaskModel>())
        { }

        public WkyTaskListView(ObservableCollection<TaskModel> viewModel)
        {
            InitializeComponent();

            this.ViewModel = viewModel;
            this.ViewModel = Aria2ApiManager.Instance.TaskList;

            this.AddTaskButton.IsEnabled = Aria2ApiManager.Instance.Connected;

            Aria2ApiManager.Instance.EventReceived
                .OfType<LoginResultEvent>()
                .SubscribeOnMainThread(async r =>
                {
                    if (r.IsSuccess)
                    {
                        this.AddTaskButton.IsEnabled = true;
                    }
                    else
                    {
                        this.AddTaskButton.IsEnabled = false;
                    }

                });

        }

        private List<TaskModel> _selectedItems;


        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ObservableCollection<TaskModel>), typeof(WkyTaskListView));

        public ObservableCollection<TaskModel> ViewModel
        {
            get { return (ObservableCollection<TaskModel>)GetValue(ViewModelProperty); }
            set
            {
                SetValue(ViewModelProperty, value);
                if (value != null && value.Count > 0)
                {
                    MainDataGrid.SelectedItem = value.First();
                }
            }
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }


        private void MainDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

        }

        private async void MenuCopyTitle_Click(object sender, RoutedEventArgs e)
        {
            MainDataGrid.SelectedItem = null;
            try
            {
                var selectedItems = _selectedItems;
                var title = "";
                foreach (var item in selectedItems)
                {
                    title += item.SubscriptionName;
                    if (item != selectedItems.Last())
                    {
                        title += "\n";
                    }
                }
                Clipboard.SetDataObject(title);
                MainWindow.Instance.ShowSnackbar("已复制标题", $"{title}");
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private async void MenuRestart_Click(object sender, RoutedEventArgs e)
        {
            MainDataGrid.SelectedItem = null;
            try
            {
                var selectedItems = _selectedItems;
                foreach (var item in selectedItems)
                {
                    if (item.Data.Status == Aria2ApiManager.KARIA2_STATUS_PAUSED ||
                            item.Data.Status == Aria2ApiManager.KARIA2_STATUS_ERROR)
                    {
                        await Aria2ApiManager.Instance.UnpauseTask(item.Data.Gid);
                    }
                }
                await Aria2ApiManager.Instance.UpdateTask();

            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private async void MenuStop_Click(object sender, RoutedEventArgs e)
        {
            MainDataGrid.SelectedItem = null;
            try
            {
                var selectedItems = _selectedItems;
                foreach (var item in selectedItems)
                {
                    if (item.Data.Status != Aria2ApiManager.KARIA2_STATUS_COMPLETE)
                    {
                        await Aria2ApiManager.Instance.PauseTask(item.Data.Gid);
                    }
                }

                await Aria2ApiManager.Instance.UpdateTask();

            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private async void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _selectedItems;

            var title = "";
            foreach (var item in selectedItems)
            {
                title += item.SubscriptionName;
                if (item != selectedItems.Last())
                {
                    title += "\n";
                }
            }

            var content = "";
            if (selectedItems.Count == 1) 
            {
                content = $"是否确认删除任务：\r\n{title}？";
            }
            else
            {
                content = $"是否确认删除{selectedItems.Count}个任务？";
            }

            await MainWindow.Instance.ShowMessageBox("提示", content, async () => {
                try
                {
                    MainDataGrid.SelectedItem = null;
                    foreach (var item in selectedItems)
                    {
                        if (item.Data.Status == Aria2ApiManager.KARIA2_STATUS_ERROR ||
                        item.Data.Status == Aria2ApiManager.KARIA2_STATUS_REMOVED ||
                        item.Data.Status == Aria2ApiManager.KARIA2_STATUS_COMPLETE)
                        {
                            await Aria2ApiManager.Instance.RemoveDownloadResult(item.Data.Gid); 
                        }
                        else
                        {
                            await Aria2ApiManager.Instance.DeleteFile(item.Data.Gid);
                            await Aria2ApiManager.Instance.RemoveDownloadResult(item.Data.Gid);
                        }
                    }

                    await Aria2ApiManager.Instance.UpdateTask();
                    MainDataGrid.Dispatcher.Invoke(() => MainDataGrid.UnselectAll());
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex);


                }
            }, () => {
                //没有操作
            });

        }

        private async void MenuDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _selectedItems;

            var title = "";
            foreach (var item in selectedItems)
            {
                title += item.SubscriptionName; //TODO
                if (item != selectedItems.Last())
                {
                    title += "\n";
                }
            }


            var content = "";
            if (selectedItems.Count == 1)
            {
                content = $"是否确认删除任务及文件：\r\n{title}？";
            }
            else
            {
                content = $"是否确认删除{selectedItems.Count}个任务及文件？";
            }



            await MainWindow.Instance.ShowMessageBox("提示", content, async () => {
                try
                {
                    foreach (var item in selectedItems)
                    {
                        await Aria2ApiManager.Instance.DeleteFile(item.Data.Gid);
                    }

                    //TODO 更新列表 Aria2ApiManager.Instance.API.UpdateTask();
                    MainDataGrid.Dispatcher.Invoke(() => MainDataGrid.UnselectAll());
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error(ex);


                }
            }, () => {
                //没有操作
            });
        }

        private async void MenuCopyLink_Click(object sender, RoutedEventArgs e)
        {
            MainDataGrid.SelectedItem = null;
            try
            {
                var url = "";
                foreach (var item in _selectedItems)
                {
                    url += item.Link;
                    url += "\n";
                }
                Clipboard.SetDataObject(url);
                MainWindow.Instance.ShowSnackbar("已复制链接", $"{url}");

            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private async void MenuShowCompletedFiles_Click(object sender, RoutedEventArgs e)
        {
            MainDataGrid.SelectedItem = null;
            try
            {
                if (!AppConfig.Instance.ConfigData.Aria2UseLocal)
                {
                    MainWindow.Instance.ShowSnackbar("无法操作", "仅支持本地Aria2模式");
                    return;
                }

                var completedItems = _selectedItems
                    .Where(a => a.Data.Status == Aria2ApiManager.KARIA2_STATUS_COMPLETE)
                    .ToList();
                if (completedItems.Count == 0)
                {
                    MainWindow.Instance.ShowSnackbar("无法操作", "仅支持已完成任务");
                    return;
                }

                var files = new List<string>();
                foreach (var item in completedItems)
                {
                    var completedFiles = await Aria2ApiManager.Instance.GetCompletedFilesAsync(item.Data.Gid);
                    if (completedFiles != null && completedFiles.Count > 0)
                    {
                        foreach (var file in completedFiles)
                        {
                            if (string.IsNullOrWhiteSpace(file))
                            {
                                continue;
                            }

                            var name = System.IO.Path.GetFileName(file);
                            files.Add(string.IsNullOrWhiteSpace(name) ? file : name);
                        }
                    }
                }

                if (files.Count == 0)
                {
                    MainWindow.Instance.ShowSnackbar("提示", "未获取到已完成文件");
                    return;
                }

                var content = string.Join("\n", files);
                await MainWindow.Instance.ShowMessageBox($"已完成文件名({files.Count})", content, null, null, null, null, "确定");
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private async void MenuAiRenameCompletedFiles_Click(object sender, RoutedEventArgs e)
        {
            MainDataGrid.SelectedItem = null;
            try
            {
                if (!AppConfig.Instance.ConfigData.Aria2UseLocal)
                {
                    MainWindow.Instance.ShowSnackbar("无法操作", "仅支持本地Aria2模式");
                    return;
                }

                if (!AiProviderClient.HasApiKey())
                {
                    MainWindow.Instance.ShowSnackbar("无法操作", "未配置AI Key");
                    return;
                }

                var completedItems = _selectedItems
                    .Where(a => a.Data.Status == Aria2ApiManager.KARIA2_STATUS_COMPLETE)
                    .ToList();
                if (completedItems.Count == 0)
                {
                    MainWindow.Instance.ShowSnackbar("无法操作", "仅支持已完成任务");
                    return;
                }

                var gidToFiles = new Dictionary<string, List<string>>();
                var inputNames = new List<string>();
                foreach (var item in completedItems)
                {
                    var completedFiles = await Aria2ApiManager.Instance.GetCompletedFilesAsync(item.Data.Gid);
                    if (completedFiles == null || completedFiles.Count == 0)
                    {
                        continue;
                    }

                    var fileList = completedFiles
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Distinct()
                        .ToList();
                    if (fileList.Count == 0)
                    {
                        continue;
                    }

                    gidToFiles[item.Data.Gid] = fileList;
                    foreach (var file in fileList)
                    {
                        var name = System.IO.Path.GetFileName(file);
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            inputNames.Add(name);
                        }
                    }
                }

                inputNames = inputNames.Distinct().ToList();
                if (inputNames.Count == 0)
                {
                    MainWindow.Instance.ShowSnackbar("提示", "未获取到已完成文件");
                    return;
                }

                var renameModels = await AutoRenameManager.GetNewNames(inputNames);
                if (renameModels == null || renameModels.Count == 0)
                {
                    MainWindow.Instance.ShowSnackbar("失败", "AI未返回有效结果");
                    return;
                }

                var renameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var model in renameModels)
                {
                    if (string.IsNullOrWhiteSpace(model.Old) || string.IsNullOrWhiteSpace(model.New))
                    {
                        continue;
                    }

                    var oldName = model.Old.Trim();
                    var newName = model.New.Trim();
                    if (!renameMap.ContainsKey(oldName))
                    {
                        var normalizedNewName = System.IO.Path.GetFileName(newName);
                        var oldExt = System.IO.Path.GetExtension(oldName);
                        if (string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(normalizedNewName)) && !string.IsNullOrWhiteSpace(oldExt))
                        {
                            normalizedNewName += oldExt;
                        }
                        renameMap.Add(oldName, normalizedNewName);
                    }
                }

                if (renameMap.Count == 0)
                {
                    MainWindow.Instance.ShowSnackbar("失败", "AI未返回有效结果");
                    return;
                }

                var renameCount = 0;
                var renameResults = new List<string>();
                foreach (var pair in gidToFiles)
                {
                    foreach (var srcPath in pair.Value)
                    {
                        var oldName = System.IO.Path.GetFileName(srcPath);
                        if (string.IsNullOrWhiteSpace(oldName))
                        {
                            continue;
                        }

                        if (!renameMap.TryGetValue(oldName, out var newName))
                        {
                            continue;
                        }

                        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var success = await Aria2ApiManager.Instance.RenameCompletedFileAsync(pair.Key, srcPath, newName);
                        if (success)
                        {
                            renameCount++;
                            renameResults.Add($"{oldName} -> {newName}");
                        }
                    }
                }

                if (renameCount > 0)
                {
                    var content = string.Join("\n", renameResults);
                    await MainWindow.Instance.ShowMessageBox($"重命名完成({renameCount})", content, null, null, null, null, "确定");
                }
                else
                {
                    MainWindow.Instance.ShowSnackbar("提示", "未产生重命名结果");
                }
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.RootNavigation.Navigate(typeof(AddTaskView), null);
            //WindowAddTask.Show(Application.Current.MainWindow);
        }

        private void MainDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //展开时，需要暂停刷新？
            var contextMenu = MainDataGrid.ContextMenu;
            contextMenu.Items.Clear();

            var selectedItems = new List<TaskModel>();
            foreach (var item in MainDataGrid.SelectedItems)
            {
                var myItem = item as TaskModel;
                if (myItem != null)
                {
                    selectedItems.Add(myItem);
                }
            }

            var jsonString = JsonConvert.SerializeObject(selectedItems);

            //需要深拷贝
            _selectedItems = JsonConvert.DeserializeObject<List<TaskModel>>(jsonString) ;

            contextMenu.Items.Clear();
            //contextMenu.Visibility
            if (_selectedItems.Count > 0)
            {
                var allCompleted = _selectedItems.All(a => a.Data.Status == Aria2ApiManager.KARIA2_STATUS_COMPLETE);
                //展示继续下载
                var showRestartMenu = _selectedItems.Any(a =>
                {
                    if (a.Data.Status == Aria2ApiManager.KARIA2_STATUS_PAUSED ||
                        a.Data.Status == Aria2ApiManager.KARIA2_STATUS_ERROR)
                    {
                        return true;
                    }
                    return false;
                });


                var showStopMenu = _selectedItems.Any(a =>
                {
                    if (a.Data.Status == Aria2ApiManager.KARIA2_STATUS_WAITING ||
                        a.Data.Status == Aria2ApiManager.KARIA2_STATUS_ACTIVE)
                    {
                        return true;
                    }
                    return false;
                });


                if (AppConfig.Instance.ConfigData.Aria2UseLocal)
                {
                    if (_selectedItems.Count == 1)
                    {
                        MenuItem menuOpenPath = new MenuItem() { Header = "打开所在目录" };
                        menuOpenPath.Click += MenuOpenPath_Click;
                        contextMenu.Items.Add(menuOpenPath);
                    }

                    if (allCompleted)
                    {
                        MenuItem menuShowCompletedFiles = new MenuItem() { Header = "查看下载文件名" };
                        menuShowCompletedFiles.Click += MenuShowCompletedFiles_Click;
                        contextMenu.Items.Add(menuShowCompletedFiles);

                        MenuItem menuAiRenameCompletedFiles = new MenuItem() { Header = "AI更名为Jellyfin格式" };
                        menuAiRenameCompletedFiles.Click += MenuAiRenameCompletedFiles_Click;
                        contextMenu.Items.Add(menuAiRenameCompletedFiles);
                    }

                }

                if (showRestartMenu)
                {
                    var menuRestart = new MenuItem() { Header = "继续下载" };
                    menuRestart.Click += MenuRestart_Click;
                    contextMenu.Items.Add(menuRestart);
                    contextMenu.Items.Add(new Separator());
                }
                else if (showStopMenu)
                {
                    MenuItem menuStop = new MenuItem() { Header = "暂停" };
                    menuStop.Click += MenuStop_Click;
                    contextMenu.Items.Add(menuStop);
                    contextMenu.Items.Add(new Separator());
                }
                else if (AppConfig.Instance.ConfigData.Aria2UseLocal && allCompleted)
                {
                    contextMenu.Items.Add(new Separator());
                }


                MenuItem menuCopyTitle = new MenuItem() { Header = "复制标题" };
                menuCopyTitle.Click += MenuCopyTitle_Click;
                contextMenu.Items.Add(menuCopyTitle);

                MenuItem menuCopyLink = new MenuItem() { Header = "复制链接" };
                menuCopyLink.Click += MenuCopyLink_Click;
                contextMenu.Items.Add(menuCopyLink);

                MenuItem menuDelete = new MenuItem() { Header = $"删除任务({selectedItems.Count})" };
                menuDelete.Click += MenuDelete_Click;
                contextMenu.Items.Add(menuDelete);

                //MenuItem menuDeleteFile = new MenuItem() { Header = $"删除任务及文件({selectedItems.Count})" };
                //menuDeleteFile.Click += MenuDeleteFile_Click;
                //contextMenu.Items.Add(menuDeleteFile);

                DataGrid row = sender as DataGrid;
                row.ContextMenu = contextMenu;
            }
        }

        private void MenuOpenPath_Click(object sender, RoutedEventArgs e)
        {
            var model = _selectedItems.FirstOrDefault();
            MainDataGrid.SelectedItem = null;
            //打开目录
            if (model != null && Directory.Exists(model.Data.Dir))
            {
                string correctedPath = System.IO.Path.GetFullPath(model.Data.Dir);
                Process.Start("explorer.exe", correctedPath);
            }
        }
    }
}
