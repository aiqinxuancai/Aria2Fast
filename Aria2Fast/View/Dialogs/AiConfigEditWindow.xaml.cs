using Aria2Fast.Service;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Aria2Fast.View.Dialogs
{
    /// <summary>
    /// AiConfigEditWindow.xaml 的交互逻辑：新增/编辑单组 AI 配置
    /// </summary>
    public partial class AiConfigEditWindow : Wpf.Ui.Controls.FluentWindow
    {
        /// <summary>
        /// 工作副本，保存时再写回，避免取消时污染原配置
        /// </summary>
        public AiConfig EditingConfig { get; }

        private bool _initializing = true;

        public AiConfigEditWindow(AiConfig source)
        {
            EditingConfig = source?.Clone() ?? new AiConfig();

            // 新建时给一个合理的默认 baseUrl / 模型
            if (string.IsNullOrWhiteSpace(EditingConfig.BaseUrl))
            {
                var option = AiProtocol.GetOption(EditingConfig.Protocol);
                EditingConfig.BaseUrl = option.DefaultBaseUrl;
                if (string.IsNullOrWhiteSpace(EditingConfig.ModelName))
                {
                    EditingConfig.ModelName = option.DefaultModel;
                }
            }

            InitializeComponent();
            DataContext = EditingConfig;

            PresetComboBox.ItemsSource = AiProtocol.Presets;

            ProtocolComboBox.ItemsSource = AiProtocol.Options;
            ProtocolComboBox.SelectedItem = AiProtocol.Options.FirstOrDefault(o => o.Type == EditingConfig.Protocol)
                ?? AiProtocol.Options[0];

            _initializing = false;
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || PresetComboBox.SelectedItem is not AiProtocol.Preset preset)
            {
                return;
            }

            EditingConfig.BaseUrl = preset.BaseUrl;
            EditingConfig.ModelName = preset.ModelName;
            EditingConfig.Protocol = preset.Protocol;

            // 同步协议下拉框
            ProtocolComboBox.SelectedItem = AiProtocol.Options.FirstOrDefault(o => o.Type == preset.Protocol)
                ?? AiProtocol.Options[0];

            // 名称为空或仍是默认时，用预设名补全
            if (string.IsNullOrWhiteSpace(EditingConfig.Name) || EditingConfig.Name == "新配置")
            {
                EditingConfig.Name = preset.DisplayName;
            }
        }

        private void ProtocolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProtocolComboBox.SelectedItem is not AiProtocol.ProtocolOption option)
            {
                return;
            }

            EditingConfig.Protocol = option.Type;

            // 初始化阶段不覆盖用户已有数据；切换协议时若字段为空或仍是默认值，自动补默认地址/模型
            if (_initializing)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingConfig.BaseUrl) || IsKnownDefaultBaseUrl(EditingConfig.BaseUrl))
            {
                EditingConfig.BaseUrl = option.DefaultBaseUrl;
            }

            if (string.IsNullOrWhiteSpace(EditingConfig.ModelName) || IsKnownDefaultModel(EditingConfig.ModelName))
            {
                EditingConfig.ModelName = option.DefaultModel;
            }
        }

        private static bool IsKnownDefaultBaseUrl(string baseUrl)
        {
            return AiProtocol.Options.Any(o => string.Equals(o.DefaultBaseUrl, baseUrl?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKnownDefaultModel(string model)
        {
            return AiProtocol.Options.Any(o => string.Equals(o.DefaultModel, model?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditingConfig.Name))
            {
                EditingConfig.Name = "未命名配置";
            }

            if (string.IsNullOrWhiteSpace(EditingConfig.BaseUrl))
            {
                System.Windows.MessageBox.Show("请填写 API 地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingConfig.ModelName))
            {
                System.Windows.MessageBox.Show("请填写模型名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingConfig.ApiKey))
            {
                System.Windows.MessageBox.Show("请填写 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
