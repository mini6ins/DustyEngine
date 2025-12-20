using System.Runtime.CompilerServices;

namespace DustyEngine
{
    public static class Debug
    {
        private static readonly List<string> LogMessages = [];
        private const string LogFilePath = "debug.log";

        private static LogLevel _currentLogLevel = LogLevel.Info;
        private static bool _writeToConsole = true;
        private static bool _writeToFile = true;
        private static bool _isDebugMode;

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            FatalError,
        }

        public static void Log(
            object? message,
            LogLevel level = LogLevel.Info,
            bool isDebugMessage = false,
            string? source = null,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0
        )
        {
            if (string.IsNullOrEmpty(source))
            {
                source = DetermineSource(file);
            }

            var formattedMessage =
                $"[{source}] " +
                $"[{DateTime.Now:HH:mm:ss}] " +
                $"[{level}] " +
                $"({Path.GetFileName(file)}:{line} in {caller}) {message}";

            if (_writeToFile)
                File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine);

            if ((int)level < (int)GetLogLevel())
                return;

            LogMessages.Add(formattedMessage);

            if (!_isDebugMode && isDebugMessage)
                return;

            if (!_writeToConsole)
                return;

            Console.WriteLine(formattedMessage);

            Console.Out.Flush();
        }

        private static string DetermineSource(string filePath)
        {
            return filePath.Contains("GraphicsEngineOpenGL") ? "Editor" : "Engine";
        }


        public static void SetLogLevel(LogLevel level) => _currentLogLevel = level;
        public static LogLevel GetLogLevel() => _currentLogLevel;
        public static void EnableConsoleLogging(bool enabled) => _writeToConsole = enabled;
        public static void EnableFileLogging(bool enabled) => _writeToFile = enabled;
        public static void EnableDebugMode(bool enabled) => _isDebugMode = enabled;
        public static void ShowLogs() => LogMessages.ForEach(Console.WriteLine);

        public static void ClearLogs()
        {
            LogMessages.Clear();
            File.WriteAllText(LogFilePath, string.Empty);
        }
    }
}
