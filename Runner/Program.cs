using GraphicsEngineOpenGL;
using WindowEngine;

namespace DustyEngine.Runner;

internal static class Program
{

    private static string _projectPath = null!;

    private static RenderMode _renderMode = RenderMode.Standalone;

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Log("Usage: DustyEngine.Runner <ProjectPath> [RenderMode]", Debug.LogLevel.FatalError);
            return 2;
        }
        
        _projectPath = args[0];
        
        if (args.Length >= 2)
        {
            if (!Enum.TryParse(args[1], true, out _renderMode))
            {
                Console.Error.WriteLine();
                Debug.Log($"Invalid RenderMode '{args[1]}'. Using default: {_renderMode}", Debug.LogLevel.FatalError);
            }
        }
        
        Console.CancelKeyPress += (_, e) => e.Cancel = true;
        
        try
        {
            var engine = new DustyEngine();
            DustyEngine.StartEngine(_projectPath, _renderMode);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
