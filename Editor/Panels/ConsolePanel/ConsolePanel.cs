using System.Collections.Concurrent;
using System.Numerics;
using DustyEngine;
using ImGuiNET;

namespace Editor.Panels.ConsolePanel;

public class ConsolePanel : IRenderablePanel
{
    private class LogEntry(string text, Debug.LogLevel level)
    {
        public string Text { get; } = text;
        public Debug.LogLevel Level { get; } = level;
    }

    private static readonly ConcurrentQueue<LogEntry> Lines = new();
    private const bool AutoScroll = true;
    private static ConsoleInterceptor? _interceptor;

    public static bool DebugEnabled;
    private static Action<bool>? _onDebugModeChanged;

    public static void InitializeConsoleInterceptor(Action<bool> onDebugModeChanged, bool currentDebugState)
    {
        DebugEnabled = currentDebugState;
        _onDebugModeChanged += onDebugModeChanged;

        var originalOutput = Console.Out;
        _interceptor = new ConsoleInterceptor(originalOutput, OnConsoleLineWritten);
        Console.SetOut(_interceptor);

        Console.WriteLine("[Editor] Console interceptor initialized");
    }

    private static void OnConsoleLineWritten(string line) =>
        Lines.Enqueue(new LogEntry(line, DetectLogLevel(line)));

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(420, 260), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Console"))
        {
            ImGui.End();
            return;
        }

        if (ImGui.Button("Clear"))
        {
            while (Lines.TryDequeue(out _)) { }
        }

        ImGui.SameLine();

        if (ImGui.Checkbox("Debug", ref DebugEnabled))
            _onDebugModeChanged?.Invoke(DebugEnabled);

        ImGui.SameLine();
        ImGui.TextDisabled($"Lines: {Lines.Count}");

        ImGui.Separator();

        ImGui.BeginChild("ConsoleScroll", new Vector2(0, 0), ImGuiChildFlags.None,
            ImGuiWindowFlags.HorizontalScrollbar);

        foreach (var entry in Lines.ToArray())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, GetColorForLevel(entry.Level));
            ImGui.TextUnformatted(entry.Text);
            ImGui.PopStyleColor();
        }

        if (AutoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            ImGui.SetScrollHereY(1.0f);

        ImGui.EndChild();
        ImGui.End();
    }

    private static Debug.LogLevel DetectLogLevel(string text)
    {
        if (text.Contains("[FatalError]") || text.Contains("[Fatal]"))
            return Debug.LogLevel.FatalError;

        if (text.Contains("[Error]"))
            return Debug.LogLevel.Error;

        return text.Contains("[Warning]") ? Debug.LogLevel.Warning : Debug.LogLevel.Info;
    }

    private static Vector4 GetColorForLevel(Debug.LogLevel level)
    {
        return level switch
        {
            Debug.LogLevel.Info => new Vector4(0.8f, 0.8f, 0.8f, 1.0f),
            Debug.LogLevel.Warning => new Vector4(1.0f, 0.65f, 0.0f, 1.0f),
            Debug.LogLevel.Error => new Vector4(1.0f, 0.3f, 0.3f, 1.0f),
            Debug.LogLevel.FatalError => new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
        };
    }

    public static void Shutdown()
    {
        if (_interceptor == null) return;
        Console.SetOut(Console.Out);
        _interceptor = null;
    }
}