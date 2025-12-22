using System.Text;

namespace Editor.Panels.ConsolePanel;

public class ConsoleInterceptor(TextWriter originalOutput, Action<string> onLineWritten) : TextWriter
{
    private readonly StringBuilder _lineBuffer = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        originalOutput.Write(value);

        if (value == '\n')
        {
            var line = _lineBuffer.ToString().TrimEnd('\r');
            if (!string.IsNullOrEmpty(line))
                onLineWritten(line);

            _lineBuffer.Clear();
        }
        else if (value != '\r')
            _lineBuffer.Append(value);
    }

    public override void WriteLine(string? value)
    {
        originalOutput.WriteLine(value);

        if (!string.IsNullOrEmpty(value))
            onLineWritten(value);
    }

    public override void Flush()
    {
        originalOutput.Flush();

        if (_lineBuffer.Length <= 0) return;

        var line = _lineBuffer.ToString();
        onLineWritten(line);
        _lineBuffer.Clear();
    }
}
