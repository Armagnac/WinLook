using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WinLook
{
    public static class Common
    {
        private const String RunOnStartupsubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private const Int32 LogFileMaxSize = 1048576;
        
        public static String[] CommandLineArguments;

        public static String CommandLine => String.Join(" ", CommandLineArguments.Select(argument => $"\"{argument}\""));

        public static readonly String ExecutablePath = Process.GetCurrentProcess().MainModule.FileName;

        public static readonly String ExecutableName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");

        public static String ExecutableDirectory => Path.GetDirectoryName(ExecutablePath);

        public static Boolean RunOnStartup
        {
            get
            {
                try
                {
                    var startupKey = Registry.CurrentUser.OpenSubKey(RunOnStartupsubKey);

                    return (startupKey?.GetValue(ExecutableName) as String)?.Contains(ExecutablePath) ?? false;
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    var startupKey = Registry.CurrentUser.OpenSubKey(RunOnStartupsubKey, true);

                    if (value)
                        startupKey?.SetValue(ExecutableName, ExecutablePath);
                    else
                        startupKey?.DeleteValue(ExecutableName, false);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static String _ProgramName;

        private static Mutex _SingleExecutionMutex;

        private static Dispatcher _Dispatcher;

        private static Boolean _Initialized;

        public static void Initialize(Application application, String programName, String[] arguments, String singleInstanceGuid)
        {
            _Dispatcher = application.Dispatcher;
            _ProgramName = programName;
            CommandLineArguments = arguments.ToArray();

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
#endif

            try
            {
                Boolean createdNew;
                _SingleExecutionMutex = new Mutex(true, singleInstanceGuid, out createdNew);
                if (!createdNew)
                {
                    var existingProcess = Process.GetProcessesByName(ExecutableName).FirstOrDefault(process => process != Process.GetCurrentProcess());
                    if (existingProcess != null)
                        Win32Api.ShowWindow(existingProcess.MainWindowHandle);

                    Environment.Exit(-1);
                }
            }
            catch (Exception exception)
            {
                ShowExceptionMessageBox(MessageBoxButton.OK, "Failed acquiring single execution mutex. Exiting...", exception);
                Environment.Exit(-1);
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, args) => CloseApplication();
        }

        private static void CloseApplication()
        {
            if (!_Initialized)
                return;

            _Initialized = false;

            if (_SingleExecutionMutex != null)
            {
                try
                {
                    _SingleExecutionMutex.ReleaseMutex();
                    _SingleExecutionMutex.Close();
                }
                catch
                {
                    // ignored
                }

                _SingleExecutionMutex = null;
            }
        }

        private static void CurrentDomainOnUnhandledException(Object sender, UnhandledExceptionEventArgs exception)
        {
            try
            {
                ShowExceptionMessageBox(MessageBoxButton.OK, "Congratulations! You've found a bug!", (Exception)exception.ExceptionObject);
                ShowMessageBox(MessageBoxButton.YesNo, MessageBoxImage.Question, $"With your permission, {_ProgramName} will now crash.");

                CloseApplication();
            }
            catch (Exception)
            {
                // ignored
            }

            Environment.Exit(-1);
        }

        public static void RunFromSTAThread(Action method)
        {
            if (_Dispatcher.CheckAccess())
                method.Invoke();
            else
                _Dispatcher.Invoke(method);
        }

        public static T RunFromSTAThread<T>(Func<T> method)
        {
            return _Dispatcher.CheckAccess() ? method.Invoke() : _Dispatcher.Invoke(method);
        }

        public static MessageBoxResult ShowMessageBox(MessageBoxButton buttons, MessageBoxImage image, String message)
        {
            return RunFromSTAThread(() => MessageBox.Show(message, _ProgramName, buttons, image));
        }

        public static MessageBoxResult ShowErrorMessageBox(MessageBoxButton buttons, String message)
        {
            return RunFromSTAThread(() => MessageBox.Show($"{message}", _ProgramName, buttons, MessageBoxImage.Error));
        }

        public static MessageBoxResult ShowExceptionMessageBox(MessageBoxButton buttons, String message, Exception exception)
        {
            return RunFromSTAThread(() => MessageBox.Show($"{message}\n\n{exception}", _ProgramName, buttons, MessageBoxImage.Error));
        }

        public static void WriteToLog(params String[] lines)
        {
            String logFilePath;
            try
            {
                logFilePath = Path.Combine(ExecutableDirectory, $"{_ProgramName}.log");
            }
            catch
            {
                return;
            }

            try
            {
                var logFileInfo = new FileInfo(logFilePath);
                if (logFileInfo.Length> LogFileMaxSize)
                {
                    var logLines = File.ReadAllLines(logFilePath);
                    File.WriteAllLines(logFilePath, logLines.Skip(100));
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                var separatedLines = lines
                    .SelectMany(line => line.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(partialLine => partialLine.Split(new[] { "\r" }, StringSplitOptions.RemoveEmptyEntries)))
                    .ToArray();

                var outputLines = new List<String> { $"{DateTime.Now.ToLocalTime()}: {separatedLines[0]}" };
                outputLines.AddRange(separatedLines.Skip(1).Select(line => $"    {line}"));
                File.AppendAllLines(logFilePath, outputLines);
            }
            catch (IOException)
            {
                // ignored
            }
        }
    }
}