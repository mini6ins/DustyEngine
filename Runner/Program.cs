using System.Runtime.InteropServices;
using GraphicsEngineOpenGL;

namespace DustyEngine.Runner;

internal static class Program
{
    private static volatile bool _stopping;
    private static string projectPath = "/home/maksym/DustyEngine/TestProject";

    private static RenderMode renderMode = RenderMode.Standalone;

    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: DustyEngine.Runner <ProjectPath> [RenderMode]");
            return 2;
        }


        if (args.Length >= 2)
        {
            if (!Enum.TryParse(args[1], true, out renderMode))
            {
                Console.Error.WriteLine($"Invalid RenderMode '{args[1]}'. Using default: {renderMode}");
            }
        }

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _stopping = true;
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => _stopping = true;
        }

        try
        {
            var engine = new DustyEngine();
            engine.StartEngine(projectPath, renderMode);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}