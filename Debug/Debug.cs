using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DustyEngine;

public static class Debug
{
    private static readonly ConcurrentQueue<string> LogMessages = new();
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
            source = DetermineSource(file);

        var formattedMessage =
            $"[{source}] " +
            $"[{DateTime.Now:HH:mm:ss}] " +
            $"[{level}] " +
            $"({Path.GetFileName(file)}:{line} in {caller}) {message}";

        if (_writeToFile) File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine);

        if ((int)level < (int)GetLogLevel()) return;

        LogMessages.Enqueue(formattedMessage);

        if (!_isDebugMode && isDebugMessage) return;
        if (!_writeToConsole) return;

        Console.WriteLine(formattedMessage);
        Console.Out.Flush();
    }

    private static string DetermineSource(string filePath) =>
        filePath.Contains("WindowEngine") ? "Editor" : "Engine";

    public static void SetLogLevel(LogLevel level) => _currentLogLevel = level;
    public static LogLevel GetLogLevel() => _currentLogLevel;
    public static void EnableConsoleLogging(bool enabled) => _writeToConsole = enabled;
    public static void EnableFileLogging(bool enabled) => _writeToFile = enabled;
    public static void EnableDebugMode(bool enabled) => _isDebugMode = enabled;
    public static void ShowLogs() => LogMessages.ToList().ForEach(Console.WriteLine);

    public static IReadOnlyList<string> GetMessages() => LogMessages.ToArray();

    public static void ClearLogs()
    {
        while (LogMessages.TryDequeue(out _)) { }
        File.WriteAllText(LogFilePath, string.Empty);
    }
}