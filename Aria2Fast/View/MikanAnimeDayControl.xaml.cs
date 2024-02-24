using Aria2Fast.Service.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// MikanAnimeDayControl.xaml 的交互逻辑
    /// </summary>
    public partial class MikanAnimeDayControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
        public double ListViewWidth { get; set; }

        public MikanAnimeDayControl()
        {
            //DataContext = this;
            InitializeComponent();
        }

        

    }
}
