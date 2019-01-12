using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace WinLook
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const String SingleExecutionMutexGuid = "{7B1519D5-82D1-42F4-9A9A-C9BE1EC7AEB4}";

        private TaskbarIcon _NotifyIcon;

        protected override void OnStartup(StartupEventArgs eventArgs)
        {
            base.OnStartup(eventArgs);

            Common.Initialize(this, "WinLook", eventArgs.Args, SingleExecutionMutexGuid);

            _NotifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        protected override void OnExit(ExitEventArgs eventArgs)
        {
            _NotifyIcon.Dispose();
            base.OnExit(eventArgs);
        }
    }
}
