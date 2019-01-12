using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NHotkey.Wpf;
using WinLook.Properties;

namespace WinLook
{
	public class NotifyIconViewModel : INotifyPropertyChanged
	{
		private const String UsageHistoryFilePath = "WinLook.dat";
		private static readonly TimeSpan AutoUpdateInterval = TimeSpan.FromSeconds(60);
		private const Int32 HungMessageTimeout = 500;
		private const Int32 IconMessageTimeout = 100;

		private Dictionary<String, Int32> _UsageHistory;

		public String Version
		{
			get
			{
				var versionString = Assembly.GetEntryAssembly().GetName().Version.ToString();
				return versionString.Substring(0, versionString.Length - 2);
			}
		}

		public Boolean RunOnStartUp
		{
			get { return Common.RunOnStartup; }
			set
			{
				Common.RunOnStartup = value;

				if (Common.RunOnStartup != value)
					Common.ShowErrorMessageBox(MessageBoxButton.OK, $"Failed {(value ? "registering to" : "unregistering from")} running on start-up");

				NotifyPropertyChanged();
			}
		}

		public Boolean SortByUsage
		{
			get { return Settings.Default.SortByUsage; }
			set
			{
				Settings.Default.SortByUsage = value;
				Settings.Default.Save();

				if (_UpdateWindowsTask == null)
					Windows = SortWindows(_Windows);

				NotifyPropertyChanged();
			}
		}

		public Boolean CloseWithShiftDelete
		{
			get { return Settings.Default.CloseWithShiftDelete; }
			set
			{
				Settings.Default.CloseWithShiftDelete = value;
				Settings.Default.Save();

				NotifyPropertyChanged();
			}
		}

		private List<WindowDataViewModel> _Windows = new List<WindowDataViewModel>();

		public List<WindowDataViewModel> Windows
		{
			get { return _Windows; }
			set
			{
				if (_Windows.SequenceEqual(value))
					return;

				_Windows = value;
				if (_MainWindow != null)
					_MainWindow.Windows = _Windows;
			}
		}


		public ICommand ShowCommand { get; private set; }
		public ICommand AboutCommand { get; private set; }
		public ICommand ExitCommand { get; private set; }

		private MainWindow _MainWindow;
		private AboutWindow _AboutWindow;

		private Boolean _FirstWindow = true;

		private Double _MainWindowTop;
		private Double _MainWindowLeft;
		private Int32 _SelectedWindowIndex;

		private Task _UpdateWindowsTask;

		private readonly HashSet<IntPtr> _FlashingWindows;

		private readonly HooksWindow _HooksWindow;
		private readonly Timer _UpdateTimer;

		public NotifyIconViewModel()
		{
			if (Settings.Default.FirstTime)
			{
				RunOnStartUp = true;
				Settings.Default.FirstTime = false;
				Settings.Default.Save();
			}

			_FlashingWindows = new HashSet<IntPtr>();

			_HooksWindow = new HooksWindow(
				windowHandle => _FlashingWindows.RemoveWhere(otherWindowHandle => otherWindowHandle == windowHandle),
				windowHandle => _FlashingWindows.Add(windowHandle));

			_HooksWindow.Show();
			_HooksWindow.Hide();

			LoadUsageHistory();
			UpdateWindows();

			ShowCommand = new RelayCommand(parameter => ShowFromTray());
			AboutCommand = new RelayCommand(parameter =>
			{
				if (_AboutWindow == null)
				{
					_AboutWindow = new AboutWindow();
					EventHandler onClose = null;
					onClose = (sender, args) =>
					{
						_AboutWindow.Closed -= onClose;
						_AboutWindow = null;
					};
					_AboutWindow.Closed += onClose;
				}

				_AboutWindow.Show();
				_AboutWindow.Activate();
				Win32Api.SetForegroundWindow(new WindowInteropHelper(_AboutWindow).Handle);
			});
			ExitCommand = new RelayCommand(parameter =>
			{
				_MainWindow?.Close();
				Application.Current.Shutdown();
			});
			Application.Current.Exit += (sender, args) => SaveUsageHistory();

			try
			{
				HotkeyManager.Current.AddOrReplace("WinLook", Key.W, ModifierKeys.Windows, (sender, args) => ShowFromTray());
			}
			catch
			{
				try
				{
					HotkeyManager.Current.AddOrReplace("WinLook", Key.W, ModifierKeys.Windows | ModifierKeys.Shift, (sender, args) => ShowFromTray());
				}
				catch
				{
				}
            }

            _UpdateTimer = new Timer(AutoUpdateInterval.TotalMilliseconds) { AutoReset = true };
            _UpdateTimer.Elapsed += (sender, args) => UpdateWindows();
            _UpdateTimer.Start();
        }

        private void SaveUsageHistory()
        {
            try
            {
                using (var fileStream = new FileStream(UsageHistoryFilePath, FileMode.Create))
                    ProtoBuf.Serializer.Serialize(fileStream, _UsageHistory);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LoadUsageHistory()
        {
            try
            {
                if (File.Exists(UsageHistoryFilePath))
                {
                    using (var fileStream = new FileStream(UsageHistoryFilePath, FileMode.Open))
                        ProtoBuf.Serializer.Deserialize<Dictionary<String, Int32>>(fileStream);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            if (_UsageHistory == null)
                _UsageHistory = new Dictionary<String, Int32>();
        }

        public Task UpdateWindows()
        {
            if (_UpdateWindowsTask != null)
                return _UpdateWindowsTask;

            _UpdateWindowsTask = Task.Run(() =>
            {
                var newWindows = new List<WindowData>();

                Action<IntPtr> addWindow = windowHandle =>
                {
                    var windowTitle = Win32Api.GetWindowTitle(windowHandle);

                    var windowIcon = GetWindowIcon(windowHandle);

                    windowIcon?.Freeze();
                    var flashing = GetFlashingWindowState(windowHandle);

                    newWindows.Add(new WindowData(windowHandle, windowIcon, windowTitle, flashing));
                    if (!_UsageHistory.ContainsKey(windowTitle.ToLower()))
                        _UsageHistory[windowTitle.ToLower()] = 0;
                };

                Win32Api.EnumWindowDelegate enumWindowCallback = (windowHandle, lParam) =>
                {
                    try
                    {
                        if (IsAllowedWindow(windowHandle))
                        {
                            var currentProcessId = Win32Api.GetWindowProcessId(windowHandle);
                            var windowTitle = Win32Api.GetWindowTitle(windowHandle);
                            if (Process.GetCurrentProcess().Id != currentProcessId && !String.IsNullOrWhiteSpace(windowTitle))
                                addWindow(windowHandle);
                        }
                    }
                    catch
                    {

                    }
                    return true;
                };

                Win32Api.EnumDesktopWindows(IntPtr.Zero, enumWindowCallback, IntPtr.Zero);

                Common.RunFromSTAThread(() =>
                {
                    Windows = SortWindows(newWindows.Select(window => new WindowDataViewModel(window)).ToList());
                    _UpdateWindowsTask = null;
                });
            });

            return _UpdateWindowsTask;
        }

        public static ImageSource GetWindowIcon(IntPtr windowHandle)
        {
            var icon = GetIconWithSendMessageGetIcon(windowHandle, GetIconFlags.IconSmall2) ??
                        GetIconWithSendMessageGetIcon(windowHandle, GetIconFlags.IconSmall) ??
                        GetIconWithSendMessageGetIcon(windowHandle, GetIconFlags.IconBig) ??
                        GetIconWithGetClassLong(windowHandle, GetClassLongIndices.HIconSmall) ??
                        GetIconWithGetClassLong(windowHandle, GetClassLongIndices.HIcon) ??
                        GetIconWithMessageQueryDragIcon(windowHandle);

            return icon;
            /*if (icon != null)
                return icon;

            var processId = Win32Api.GetWindowProcessId(windowHandle);
            var process = Process.GetProcessById(processId);

            try
            {
                return Win32Api.GetIconFromFile(process.MainModule.FileName, true);
            }
            catch (Exception)
            {
                // ignored.
            }

            return null;*/
        }

        private static ImageSource GetIconWithSendMessageGetIcon(IntPtr windowHandle, GetIconFlags iconFlags)
        {
            IntPtr iconHandle;
            Win32Api.SendMessageTimeout(
                windowHandle,
                WindowMessages.GetIcon,
                (Int32)iconFlags,
                0,
                SendMessageTimeoutFlags.AbortIfHung,
                IconMessageTimeout,
                out iconHandle);

            return Win32Api.GetIcon(iconHandle);
        }

        private static ImageSource GetIconWithGetClassLong(IntPtr windowHandle, GetClassLongIndices index)
        {
            var iconHandle = Win32Api.GetClassLong(windowHandle, index);

            return Win32Api.GetIcon(iconHandle);
        }

        private static ImageSource GetIconWithMessageQueryDragIcon(IntPtr windowHandle)
        {
            IntPtr iconHandle;
            Win32Api.SendMessageTimeout(
                windowHandle,
                WindowMessages.QueryDragIcon,
                0,
                0,
                SendMessageTimeoutFlags.AbortIfHung,
                IconMessageTimeout,
                out iconHandle);

            return Win32Api.GetIcon(iconHandle);
        }

        private Boolean GetFlashingWindowState(IntPtr windowHandle)
        {
            return _FlashingWindows.Contains(windowHandle);
        }

        private List<WindowDataViewModel> SortWindows(IEnumerable<WindowDataViewModel> windows)
        {
            if (SortByUsage)
            {
                return windows.OrderByDescending(window => _UsageHistory[window.Title.ToLower()])
                    .ThenBy(window => window.Title, new StringLogicalComparer()).ToList();
            }

            return windows.OrderBy(window => window.Title, new StringLogicalComparer()).ToList();
        }

        private static Boolean IsAllowedWindow(IntPtr windowHandle)
        {
            try
            {
                if (!Win32Api.IsWindowVisible(windowHandle))
                    return false;

                IntPtr iconHandle;
                var sendMessageResult = Win32Api.SendMessageTimeout(
                    windowHandle,
                    WindowMessages.GetIcon,
                    (Int32)GetIconFlags.IconSmall,
                    0,
                    SendMessageTimeoutFlags.AbortIfHung,
                    HungMessageTimeout,
                    out iconHandle);
                if (sendMessageResult == IntPtr.Zero)
                    return false;

                var cacheRequest = new CacheRequest { AutomationElementMode = AutomationElementMode.Full };
                cacheRequest.Add(AutomationElement.ControlTypeProperty);
                cacheRequest.Push();

                var windowElement = AutomationElement.FromHandle(windowHandle);

                cacheRequest.Pop();
                if (!windowElement.Cached.ControlType.Equals(ControlType.Window))
                    return false;

                try
                {
                    if (AutomationElement.RootElement != TreeWalker.ControlViewWalker.GetParent(windowElement))
                        return false;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                // ignored.
            }

            return true;
        }

        public void ShowFromTray()
        {
            UpdateWindows();

            if (_MainWindow == null)
            {
                _MainWindow = new MainWindow(Windows, _UsageHistory, UpdateWindows);

                CancelEventHandler onClosing = (sender, args) =>
                {
                    _MainWindowLeft = _MainWindow.Left;
                    _MainWindowTop = _MainWindow.Top;
                    _SelectedWindowIndex = _MainWindow.SelectedWindowIndex;
                };

                EventHandler onClose = null;
                onClose = (sender, args) =>
                {
                    _MainWindow.Closing -= onClosing;
                    _MainWindow.Closed -= onClose;
                    _MainWindow = null;
                };
                _MainWindow.Closing += onClosing;
                _MainWindow.Closed += onClose;

                if (!_FirstWindow)
                {
                    _MainWindow.Left = _MainWindowLeft;
                    _MainWindow.Top = _MainWindowTop;
                    _MainWindow.SelectedWindowIndex = _SelectedWindowIndex;
                }

                _FirstWindow = false;
            }

            try
            {
                _MainWindow.Show();
                _MainWindow.Activate();
                Win32Api.SetForegroundWindow(new WindowInteropHelper(_MainWindow).Handle);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"An error occurred when showing WinLook:\n{exception.Message}");
            }
            
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private class StringLogicalComparer : IComparer<String>
        {
            public Int32 Compare(String first, String second)
            {
                return Win32Api.StrCmpLogicalW(first, second);
            }
        }
    }
}
