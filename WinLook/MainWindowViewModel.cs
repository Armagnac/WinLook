using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinLook.Properties;

namespace WinLook
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const Int32 CloseMessageTimeout = 2500;
        private const Char FlashWindowPrefix = '=';

        private readonly Dictionary<String, Int32> _UsageHistory;

        private List<WindowDataViewModel> _Windows;
        public List<WindowDataViewModel> Windows
        {
            get
            {
                return _Windows
                    .Where(window => ContainsSearchText(window.Title) && (window.Flashing || !(SearchText?.StartsWith(new String(FlashWindowPrefix, 1)) ?? false)))
                    .ToList();
            }
            set
            {
                if (_Windows.SequenceEqual(value))
                    return;

                _Windows = value;
                var currentWindowIndex = SelectedWindowIndex;
                NotifyPropertyChanged();

                if (_Windows.Count > 0 && SelectedWindowIndex <= -1)
                    SelectedWindowIndex = 0;

                if (SelectedWindowIndex != currentWindowIndex)
                    SelectedWindowIndex = Math.Min(currentWindowIndex, _Windows.Count - 1);
            }
        }

        private WindowDataViewModel _SelectedWindow;
        public WindowDataViewModel SelectedWindow
        {
            get { return _SelectedWindow; }
            set
            {
                if (_SelectedWindow == value)
                    return;

                _SelectedWindow = value;
                NotifyPropertyChanged();
                _Window.WindowsListBox.ScrollIntoView(SelectedWindow);
            }
        }

        private Int32 _SelectedWindowIndex;
        public Int32 SelectedWindowIndex
        {
            get { return _SelectedWindowIndex; }
            set
            {
                if (_SelectedWindowIndex == value)
                    return;

                _SelectedWindowIndex = Math.Min(value, _Windows.Count - 1);
                NotifyPropertyChanged();
            }
        }

        private String _SearchText;
        public String SearchText
        {
            get { return _SearchText; }
            set
            {
                if (_SearchText == value)
                    return;

                _SearchText = value;
                NotifyPropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                NotifyPropertyChanged(nameof(Windows));

                if ((_Windows.Count > 0 && SelectedWindowIndex <= -1))
                    SelectedWindowIndex = 0;
            }
        }

        private readonly MainWindow _Window;
        private readonly Func<Task> _UpdateWindows;

        public MainWindowViewModel(MainWindow window, List<WindowDataViewModel> windows, Dictionary<String, Int32> usageHistory, Func<Task> updateWindows)
        {
            _Window = window;
            _Windows = windows;
            _UsageHistory = usageHistory;
            _UpdateWindows = updateWindows;

            _Window.Activated += (sender, args) => updateWindows();
            _Window.PreviewKeyDown += (sender, args) => args.Handled = HandleInput(args.Key);
            _Window.SearchTextBox.KeyUp += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                    ClearInput();
            };

            _Window.SearchTextBox.Focus();
        }

        public void OnDoubleClick()
        {
            if (ShowSelectedWindow())
                MinimizeToTray();
        }

        private static Boolean RunProcess(String commandLine)
        {
            try
            {
                var childProcess = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = commandLine });
                Win32Api.ShowWindow(childProcess?.MainWindowHandle ?? IntPtr.Zero);
            }
            catch
            {
                try
                {
                    var childProcess = Process.Start(new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = commandLine.Split().First(),
                        Arguments = String.Join(" ", commandLine.Split().Skip(1))
                    });

                    Win32Api.ShowWindow(childProcess?.MainWindowHandle ?? IntPtr.Zero);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private Boolean ShowSelectedWindow()
        {
            if (SelectedWindow == null)
                return false;

            _UsageHistory[SelectedWindow.Title.ToLower()] += 1;
            return Win32Api.ShowWindow(SelectedWindow.Handle);
        }

        private Boolean CloseSelectedWindow()
        {
            if (SelectedWindow == null)
                return false;

            Win32Api.ShowWindow(SelectedWindow.Handle);

            IntPtr result;
            Win32Api.SendMessageTimeout(
                SelectedWindow.Handle,
                WindowMessages.SystemCommand,
                (Int32)SysCommandFlags.Close,
                0,
                SendMessageTimeoutFlags.Block,
                CloseMessageTimeout,
                out result);

            return true;
        }

        private Boolean ContainsSearchText(String title)
        {
            if (String.IsNullOrWhiteSpace(SearchText))
                return true;

            var searchedStrings = SearchText.TrimStart(FlashWindowPrefix).ToLower().Split()
                .Where(word => !String.IsNullOrWhiteSpace(word)).ToList();

            return ContainsWords(title.ToLower(), searchedStrings);
        }

        private static Boolean ContainsWords(String title, List<String> words)
        {
            if (words.Count == 0)
                return true;

            foreach (var searchedString in words)
            {
                var tempList = words.ToList();
                tempList.Remove(searchedString);
                var currentIndex = 0;
                while (currentIndex < title.Length)
                {
                    var normalPossibleIndex = title.IndexOf(searchedString, currentIndex, StringComparison.InvariantCulture);
                    var languageSwitchPossibleIndex = title.IndexOf(SwitchLanguage(searchedString), currentIndex, StringComparison.InvariantCulture);

                    if (normalPossibleIndex == -1)
                        currentIndex = languageSwitchPossibleIndex;
                    else if (languageSwitchPossibleIndex == -1)
                        currentIndex = normalPossibleIndex;
                    else
                        currentIndex = Math.Min(normalPossibleIndex, languageSwitchPossibleIndex);

                    if (currentIndex == -1)
                        return false;

                    var tempString = title.Remove(currentIndex, searchedString.Length);
                    if (ContainsWords(tempString, tempList))
                        return true;

                    currentIndex += 1;

                }
            }

            return false;
        }

        private static String SwitchLanguage(String word, SwitchLanguageDirection direction = SwitchLanguageDirection.Any)
        {
            const String qwertyLayout = "qwertyuiop[]asdfghjkl;'zxcvbnm,./";
            const String hebrewLayout = "/'קראטוןםפ][שדגכעיחלךף,זסבהנמצתץ.";
            const String hebrewLetters = "אבגדהוזחטיכלמנסעפצקרשת";

            var fromLayout = qwertyLayout;
            var toLayout = hebrewLayout;
            if (word.Intersect(hebrewLetters).Any())
            {
                if (direction == SwitchLanguageDirection.OnlyToHebrew)
                    return word;
                fromLayout = hebrewLayout;
                toLayout = qwertyLayout;
            }
            else
            {
                if (direction == SwitchLanguageDirection.OnlyToEnglish)
                    return word;
            }

            return new String(word.Select(character =>
            {
                var characerIndex = fromLayout.IndexOf(character);
                if (characerIndex == -1)
                    return character;

                return toLayout[characerIndex];
            }).ToArray());
        }

        private enum SwitchLanguageDirection
        {
            Any,
            OnlyToHebrew,
            OnlyToEnglish
        }

        private Boolean HandleInput(Key key)
        {
            _Window.SearchTextBox.Focus();
            switch (key)
            {
                case Key.Enter:
                    if (Keyboard.GetKeyStates(Key.LeftCtrl).HasFlag(KeyStates.Down) || Keyboard.GetKeyStates(Key.RightCtrl).HasFlag(KeyStates.Down))
                    {
                        if (RunProcess(SearchText))
                            MinimizeToTray();
                        return true;
                    }

                    if (Windows.Any())
                    {
                        if (SelectedWindowIndex <= -1 || Windows.Count <= SelectedWindowIndex)
                            SelectedWindowIndex = 0;

                        if (ShowSelectedWindow())
                        {
                            MinimizeToTray();
                            return true;
                        }
                    }
                    else if (!String.IsNullOrWhiteSpace(SearchText))
                    {
                        if (RunProcess(SearchText))
                            MinimizeToTray();
                        return true;
                    }
                    return false;
                case Key.Delete:
                    if (Settings.Default.CloseWithShiftDelete && !Keyboard.GetKeyStates(Key.LeftShift).HasFlag(KeyStates.Down) && !Keyboard.GetKeyStates(Key.RightShift).HasFlag(KeyStates.Down))
                        return false;

                    if (!CloseSelectedWindow())
                        return false;

                    var currentSearchText = SearchText;
                    _UpdateWindows().ContinueWith(task => Common.RunFromSTAThread(() =>
                    {
                        if (!Windows.Any() && SearchText == currentSearchText)
                            ClearInput();
                    }));
                    return true;
                case Key.Up:
                    if (SelectedWindowIndex > 0)
                        SelectedWindowIndex -= 1;
                    return true;
                case Key.Down:
                    SelectedWindowIndex = (SelectedWindowIndex == -1)
                        ? 0
                        : Math.Min(Windows.Count - 1, SelectedWindowIndex + 1);
                    return true;
                case Key.PageUp:
                    if (Windows.Any())
                        SelectedWindowIndex = 0;
                    return true;
                case Key.PageDown:
                    SelectedWindowIndex = Windows.Count - 1;
                    return true;
                case Key.Escape:
                    MinimizeToTray();
                    return true;

                default:
                    if (Windows.Any() && (SelectedWindowIndex <= -1 || Windows.Count <= SelectedWindowIndex))
                        SelectedWindowIndex = 0;
                    return false;
            }
        }

        private void ClearInput()
        {
            SearchText = String.Empty;
            _Window.SearchTextBox.Focus();
        }

        public void MinimizeToTray()
        {
            _Window.Close();
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}