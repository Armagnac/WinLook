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

namespace WinLook
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public List<WindowDataViewModel> Windows
        {
            get { return _ViewModel.Windows; }
            set { _ViewModel.Windows = value; }
        }

        public Int32 SelectedWindowIndex
        {
            get { return _ViewModel.SelectedWindowIndex; }
            set { _ViewModel.SelectedWindowIndex = value; }
        }

        private readonly MainWindowViewModel _ViewModel;

        public MainWindow(List<WindowDataViewModel> windows, Dictionary<String, Int32> usageHistory, Func<Task> updateWindows)
        {
            InitializeComponent();
            _ViewModel = new MainWindowViewModel(this, windows, usageHistory, updateWindows);
            StateChanged += (sender, args) =>
            {
                if (WindowState == WindowState.Minimized)
                    _ViewModel.MinimizeToTray();
            };

            DataContext = _ViewModel;
        }

        private void ItemMouseDoubleClick(Object sender, MouseButtonEventArgs e)
        {
            _ViewModel.OnDoubleClick();
        }
    }
}
