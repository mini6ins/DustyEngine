using System.Diagnostics;
using GraphicsEngine;
using Debug = DustyEngine.Debug;

namespace PlayerRunner;

internal static class Program
{
    private static void Main(string[] args)
    {
        var projectDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");

        Debug.Log("--------------------------------------------------");
        Debug.Log("Game runner starting...");

        Debug.Log(Directory.GetCurrentDirectory());
        Debug.Log(projectDir);

        DustyEngine.DustyEngine.StartEngine(projectDir, RenderMode.Standalone);
    }
}
