﻿using LibraryInstaller.Contracts;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace LibraryInstaller.Vsix
{
    public class Logger : ILogger
    {
        private static IVsOutputWindowPane _pane;
        private static IVsOutputWindow _output = VsHelpers.GetService<SVsOutputWindow, IVsOutputWindow>();
        private static IVsActivityLog _activityLog = VsHelpers.GetService<SVsActivityLog, IVsActivityLog>();
        private static IVsStatusbar statusbar = VsHelpers.GetService<SVsStatusbar, IVsStatusbar>();

        public void Log(string message, Level level)
        {
            LogEvent(message, level);
        }

        public static void LogEvent(string message, Level level)
        {
            try
            {
                switch (level)
                {
                    case Level.Operation:
                        LogToOutputWindow(message);
                        break;
                    case Level.Error:
                        LogToActivityLog(message, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
                        break;
                    case Level.Task:
                        LogToStatusBar(message);
                        LogToOutputWindow(message);
                        break;
                    case Level.Status:
                        LogToStatusBar(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }

        public static void ClearOutputWindow()
        {
            if (_pane != null)
            {
                _pane.Clear();
            }
        }

        private static void LogToActivityLog(string message, __ACTIVITYLOG_ENTRYTYPE type)
        {
            _activityLog.LogEntry((uint)type, Vsix.Name, message);
        }

        public static void LogToStatusBar(string message)
        {
            statusbar.FreezeOutput(0);
            statusbar.SetText(message);
            statusbar.FreezeOutput(1);
        }

        private static void LogToOutputWindow(object message)
        {
            if (EnsurePane())
            {
                _pane.OutputString(message + Environment.NewLine);
            }
        }

        private static bool EnsurePane()
        {
            if (_pane == null)
            {
                var guid = Guid.NewGuid();
                _output.CreatePane(ref guid, Vsix.Name, 1, 1);
                _output.GetPane(ref guid, out _pane);
            }

            return _pane != null;
        }
    }
}
