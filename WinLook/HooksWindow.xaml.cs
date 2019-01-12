using System;
using System.Windows;
using System.Windows.Interop;

namespace WinLook
{
    /// <summary>
    /// Interaction logic for HooksWindow.xaml
    /// </summary>
    public partial class HooksWindow
    {
        public IntPtr WindowHandle;

        public Action<IntPtr> WindowActivatedAction;
        public Action<IntPtr> WindowFlashedAction;

        public HooksWindow(Action<IntPtr> windowActivatedAction, Action<IntPtr> windowFlashedAction)
        {
            InitializeComponent();

            WindowActivatedAction = windowActivatedAction;
            WindowFlashedAction = windowFlashedAction;
        }

        protected override void OnSourceInitialized(EventArgs eventArgs)
        {
            base.OnSourceInitialized(eventArgs);
            var source = (HwndSource)PresentationSource.FromVisual(this);
            if (source == null)
                return;

            source.AddHook(Hook);
            WindowHandle = source.Handle;

            if (!Win32Api.RegisterShellHookWindow(WindowHandle))
                return;
        }

        private IntPtr Hook(IntPtr handle, Int32 message, IntPtr wParam, IntPtr lParam, ref Boolean handled)
        {
            if ((wParam.ToInt64() == (Int64)ShellProcMessage.HShellWindowActivated) ||
                (wParam.ToInt64() == (Int64)ShellProcMessage.HShellRudeApplication))
            {
                WindowActivatedAction(lParam);
            }
                
            if (wParam.ToInt64() == (Int64)ShellProcMessage.HShellFlash)
                WindowFlashedAction(lParam);

            return IntPtr.Zero;
        }
    }
}