using DustyEngine;
using SceneSystem.Converters;

namespace SceneSystem;

public static class ProjectScriptService
{
    public static bool Reload(string projectPath, bool throwOnError = false)
    {
        try
        {
            var dllPath = Path.Combine(projectPath, "Settings", "Dlls", "UserScripts.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(dllPath)!);
            
            var ok = ProjectScriptCompiler.CompileAllScripts(
                projectPath,
                dllPath,
                out var errors);

            if (!ok)
            {
                foreach (var error in errors)
                    Debug.Log(error, Debug.LogLevel.Error, true);

                return throwOnError ? throw new Exception("Script compilation failed.\n" + string.Join("\n", errors)) : false;
            }

            if (File.Exists(dllPath))
                ScriptAssembly.Load(dllPath);
            
            Debug.Log($"Loaded project scripts: {dllPath}", Debug.LogLevel.Info, true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Script reload failed: {ex.Message}", Debug.LogLevel.Error, true);

            if (throwOnError)
                throw;

            return false;
        }
    }
}