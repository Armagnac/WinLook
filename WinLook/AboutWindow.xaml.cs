using System;
using System.Reflection;
using System.Windows;

namespace WinLook
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        public String Version
        {
            get
            {
                var versionString = Assembly.GetEntryAssembly().GetName().Version.ToString();
                return versionString.Substring(0, versionString.Length - 2);
            }
        }

        public AboutWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnCloseClicked(Object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}