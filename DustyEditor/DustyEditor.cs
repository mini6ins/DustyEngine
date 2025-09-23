using System.Diagnostics;

namespace DustyEditor;

internal static class DustyEditor
{
    private const string ProjectPath = "/home/maksym/github/DustyEngine/TestProject";

    static void Main(string[] args)
    {
        // DustyEngine.DustyEngine engine = new DustyEngine.DustyEngine();

        // Task.Run( () => engine.StartEngine(ProjectPath));
        //
        // Process.Start("/home/maksym/github/DustyEngine/DustyEngine/bin/Debug/net8.0/DustyEngine.dll");
        
        StartEngine();
    }

    static void StartEngine()
    {
        var runnerPath =
            "/home/maksym/github/DustyEngine/Runner/bin/Debug/net9.0/Runner";

        var psi = new ProcessStartInfo
        {
            FileName = runnerPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(ProjectPath);

        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine("[ENGINE] " + e.Data);
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.Error.WriteLine("[ENGINE-ERR] " + e.Data);
        };

        proc.Exited += (_, __) =>
        {
            Console.WriteLine($"[ENGINE] exited with code {proc.ExitCode}");
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

       
        proc.WaitForExit();
    }
}