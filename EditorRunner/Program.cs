using Avalonia;
using Avalonia.Controls;
using DustyEngine;
using GraphicsEngine;

namespace EditorRunner;

public static class Program
{
    private static string _projectPath = null!;
    private static RenderMode _renderMode = RenderMode.EditorStop;

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            var result = ShowProjectSelectorGui();
            if (!result.HasValue)
            {
                return 2;
            }

            (_projectPath, _renderMode) = result.Value;
        }
        else
        {
            _projectPath = args[0];

            if (args.Length >= 2)
            {
                if (!Enum.TryParse(args[1], true, out _renderMode))
                {
                    Console.Error.WriteLine();
                    Debug.Log($"Invalid RenderMode '{args[1]}'. Using default: {_renderMode}",
                        Debug.LogLevel.FatalError);
                }
            }
            else
            {
                _renderMode = RenderMode.EditorStop;
            }
        }

        Console.CancelKeyPress += (_, e) => e.Cancel = true;

        try
        {
            var engine = new DustyEngine.DustyEngine();

            DustyEngine.DustyEngine.StartEngine(_projectPath, _renderMode);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static (string path, RenderMode mode)? ShowProjectSelectorGui()
    {
      
        var builder = AppBuilder.Configure<ProjectSelectorApp>().UsePlatformDetect().WithInterFont().LogToTrace();
        builder.StartWithClassicDesktopLifetime([], ShutdownMode.OnMainWindowClose);

        return ProjectSelectorApp.Instance?.Result;
    }
}
