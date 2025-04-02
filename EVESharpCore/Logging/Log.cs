extern alias SC;

using EVESharpCore.Cache;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Utility.AsyncLogQueue;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Logging
{
    public static class Log
    {
        #region Fields

        public static bool ConsoleDirectoryExist = false;

        private static AsyncLogQueue _asyncLogQueue = new AsyncLogQueue();

        #endregion Fields

        #region Properties

        public static string _characterName { get; set; }
        public static AsyncLogQueue AsyncLogQueue => _asyncLogQueue;

        public static string CharacterName
        {
            get
            {
                if (string.IsNullOrEmpty(_characterName))
                    _characterName = ESCache.Instance.EveAccount.CharacterName;
                return _characterName;
            }
        }

        public static string ConsoleLogFile => Path.Combine(ConsoleLogPath,
            string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "console" + ".log");


        public static string WindowEventLogFile => Path.Combine(Logpath,
          string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "WndEvent" + ".log");

        public static string ConsoleLogPath
        {
            get
            {
                if (string.IsNullOrEmpty(_consoleLogPath))
                    _consoleLogPath = Path.Combine(Logpath, "Console\\");
                return _consoleLogPath;
            }
        }

        public static string Logpath
        {
            get
            {
                if (string.IsNullOrEmpty(_logPath))
                    _logPath = Util.AssemblyPath + "\\Logs\\" + ESCache.Instance.EveAccount.CharacterName + "\\";
                return _logPath;
            }
        }

        private static string _consoleLogPath { get; set; }
        private static string _logPath { get; set; }

        #endregion Properties

        #region Methods

        public static void CreateConsoleDirectory()
        {
            if (ConsoleLogPath != null && ConsoleLogFile != null)
                if (!string.IsNullOrEmpty(ConsoleLogFile))
                {
                    Directory.CreateDirectory(ConsoleLogPath);
                    if (Directory.Exists(ConsoleLogPath)) ConsoleDirectoryExist = true;
                }
        }

        public static string FilterPath(string path)
        {
            try
            {
                if (path == null)
                    return string.Empty;

                path = path.Replace("\"", "");
                path = path.Replace("?", "");
                path = path.Replace("\\", "");
                path = path.Replace("/", "");
                path = path.Replace("'", "");
                path = path.Replace("*", "");
                path = path.Replace(":", "");
                path = path.Replace(">", "");
                path = path.Replace("<", "");
                path = path.Replace(".", "");
                path = path.Replace(",", "");
                path = path.Replace("'", "");
                while (path.IndexOf("  ", StringComparison.Ordinal) >= 0)
                    path = path.Replace("  ", " ");
                return path.Trim();
            }
            catch (Exception exception)
            {
                WriteLine("Exception [" + exception + "]");
                return null;
            }
        }

        public static void RemoteWriteLine(string s)
        {
            WCFClient.Instance.GetPipeProxy.RemoteLog(s);
        }

        public static void WriteLine(string line, Color? col = null, [CallerMemberName] string DescriptionOfWhere = "")
        {
            //Todo: fix performance
            if (!ConsoleDirectoryExist)
                CreateConsoleDirectory();

            var consoleLogFile = ConsoleLogFile;
            if (consoleLogFile != null)
            {
                _asyncLogQueue.File = consoleLogFile;
                _asyncLogQueue.Enqueue(new LogEntry(line, DescriptionOfWhere, col));
            }
        }

        #endregion Methods
    }
}