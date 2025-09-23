using System.Runtime.InteropServices;

namespace DustyEngine.Runner;

internal static class Program
{
    private static volatile bool _stopping;

    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: DustyEngine.Runner <ProjectPath>");
            return 2;
        }

        string projectPath = args[0];
        
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
            
            engine.StartEngine(projectPath);
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
        finally
        {
            // engine.Stop(); // если есть
            // engine.Dispose(); // если есть
        }
    }
}