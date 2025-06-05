using System.Runtime.CompilerServices;

namespace DustyEngine
{
    public static class Debug
    {
        private static List<string> logMessages = new List<string>();
        private static string logFilePath = "debug.log";
        
        private static LogLevel currentLogLevel = LogLevel.Info;
        private static bool writeToConsole = true;
        private static bool writeToFile = true;
        private static bool IsDebugMode = false;

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            FatalError,
        }

        public static void Log(object? message, LogLevel level = LogLevel.Info, bool isDebugMessage = false,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            string formattedMessage = 
                $"[{DateTime.Now:HH:mm:ss}] [{level}] ({Path.GetFileName(file)}:{line} in {caller}) {message}";
    
            if (writeToFile)
                File.AppendAllText(logFilePath, formattedMessage + Environment.NewLine);

            if ((int)level >= (int)GetLogLevel())
            {
                logMessages.Add(formattedMessage);
                
                if (IsDebugMode == false && isDebugMessage == true) return;
        
                if (writeToConsole)
                {
                    Console.WriteLine(formattedMessage);
                    Console.Out.Flush();
                }
            }
        }

        public static void SetLogLevel(LogLevel level) => currentLogLevel = level;
        public static LogLevel GetLogLevel() => currentLogLevel;
        public static void EnableConsoleLogging(bool enabled) => writeToConsole = enabled;
        public static void EnableFileLogging(bool enabled) => writeToFile = enabled;
        public static void EnableDebugMode(bool enabled) => IsDebugMode = enabled;
        public static void ShowLogs() => logMessages.ForEach(Console.WriteLine);

        public static void ClearLogs()
        {
            logMessages.Clear();
            File.WriteAllText(logFilePath, string.Empty);
        }
    }
}