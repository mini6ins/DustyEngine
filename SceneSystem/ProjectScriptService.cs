using SceneSystem.Converters;

namespace DustyEngine.Core.Scripting;

public static class ProjectScriptService
{
    public static bool Reload(string projectPath, bool throwOnError = false)
    {
        try
        {
            var dllPath = Path.Combine(projectPath, "Settings", "Dlls", "UserScripts.dll");

            var ok = ProjectScriptCompiler.CompileAllScripts(
                projectPath,
                dllPath,
                out var errors);

            if (!ok)
            {
                foreach (var error in errors)
                    Debug.Log(error, Debug.LogLevel.Error, true);

                if (throwOnError)
                    throw new Exception("Script compilation failed.");

                return false;
            }

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