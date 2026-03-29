using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DustyEngine.Components;
using DustyEngine.Core.Scripting;
using DustyEngine.Scene;
using Editor;
using Editor.Panels.ConsolePanel;
using Editor.Panels.ExportProjectPanel;
using Editor.Panels.HierarchyPanel;
using Editor.Panels.ProjectFilePanel;
using Editor.Panels.ProjectSetiingPanel;
using Editor.Panels.ViewPortPanel;
using GraphicsEngine;
using SceneSystem.Converters;
using WindowEngine;

namespace DustyEngine;

public sealed class DustyEngine : IDisposable
{
    private const double EngineVersion = 0.1;

    public static string ProjectFolderPath { get; set; } = null!;
    private static ProjectSettings? _settings;
    private static Window _window = null!;

    private static readonly Action<MeshRenderer> AddRenderer = meshRenderer =>
        _window.Renderer?.AddRenderer(meshRenderer);

    private static readonly Action<MeshRenderer> RemoveRenderer =
        meshRenderer => _window.Renderer?.RemoveRendererByComponent(meshRenderer);

    public static volatile bool PendingScriptReload = false;

    private static void SetupScriptHotReload()
    {
        ScriptAssembly.OnAssemblyReloaded += () => { ScriptReloadBridge.PendingReload = true; };

        ScriptReloadBridge.OnReload += HotReloadScene;
    }
    
    public static void HotReloadScene()
    {
        if (_window.RenderMode == RenderMode.EditorRun) return;

        if (SceneManager.CurrentScene == null) return;
        var scenePath = SceneManager.CurrentScene.Path;
        if (string.IsNullOrWhiteSpace(scenePath)) return;

        GameLoop.ClearMethodCaches();

        SceneManager.LoadSceneByPath(scenePath);
        _window.LoadScene?.Invoke();
        HierarchyPanel.OnChangeScene?.Invoke();
        Debug.Log("Hot reload: scene reloaded after script change", Debug.LogLevel.Info, true);
    }
    
    
    
    public static void StartEngine(string path, RenderMode renderMode)
    {
        ProjectFolderPath = path;

        SaveProjectVersion();
        Debug.ClearLogs();

        _settings = ProjectSettings.LoadProject(path);

        if (_settings == null)
        {
            Debug.Log("Can't load project settings", Debug.LogLevel.FatalError, true);
            return;
        }

        _settings.DustyEngineVersion = EngineVersion;
        SaveEngineVersionToProjectSettings();

        SceneManager.ProjectPath = ProjectFolderPath;
        SetupDebug(renderMode);

        ProjectScriptService.Reload(ProjectFolderPath, throwOnError: true);

        if (!SceneManager.LoadSceneByPath(_settings.PathToScenes.FirstOrDefault())) return;

        if (renderMode == RenderMode.Standalone)
            GameLoop.StartLifeCycle();

        CreateWindow(renderMode);
    }

    private static void SaveProjectVersion()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "engine_version.txt");
        File.WriteAllText(path, EngineVersion.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("Saving engine_version.txt to: " + path);
    }

    private static void SaveEngineVersionToProjectSettings()
    {
        var settingsPath = Path.Combine(ProjectFolderPath, "Settings", "project_settings.json");

        var node = JsonNode.Parse(File.ReadAllText(settingsPath)) ??
                   throw new Exception("project_settings.json is empty/invalid");

        node["DustyEngineVersion"] = EngineVersion;

        File.WriteAllText(
            settingsPath,
            node.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );
    }

    private static void CreateWindow(RenderMode renderMode)
    {
        SceneManager.AddRenderer += AddRenderer;
        SceneManager.RemoveRenderer += RemoveRenderer;

        _window = new Window(
            GameLoop.ExecuteLifeCycle,
            _settings.ScreenSize.ToOpenTK(),
            _settings.ProjectName,
            _settings.PathToVertShader,
            _settings.PathToFragShader,
            _settings.Vsync,
            renderMode,
            ProjectFolderPath);

        _window.Run();
    }

    private static void SetupDebug(RenderMode renderMode)
    {
        Debug.EnableDebugMode(_settings.Debug);
        Debug.SetLogLevel(_settings.LogLevel);
        Debug.EnableConsoleLogging(_settings.LogToConsole);
        Debug.EnableFileLogging(_settings.LogToFile);

        var onDebugEnabled = Debug.EnableDebugMode;

        if (renderMode == RenderMode.EditorStop)
        {
            ConsolePanel.InitializeConsoleInterceptor(onDebugEnabled, _settings.Debug);
            RendererUI.OnProjectSave += () => ProjectSettings.SaveProject(_settings);
            ProjectSetiingPanel.OnSaveProjectSettings += () => ProjectSettings.SaveProjectSettings(_settings);
            ProjectFilePanel.OnSceneOpened += OpenScene;
            ViewportPanel.OnPlayModeChanged += ChangePlayMode;

            ProjectFileManager.OnSceneMoved += (oldPath, newPath) =>
                ProjectSettings.UpdateScenePath(_settings, oldPath, newPath);

            ExportProjectPanel.OnExportProject += outPath =>
                ProjectCompiler.ProjectCompiler.Compile(ProjectFolderPath, outPath, _settings);
            SetupScriptHotReload();
        }

        Debug.Log("Project settings loaded");
        Debug.Log($"Initial currentLogLevel: {Debug.GetLogLevel()}", Debug.LogLevel.Info, true);
        Debug.Log("Test INFO", Debug.LogLevel.Info, true);
        Debug.Log("Test WARNING", Debug.LogLevel.Warning, true);
        Debug.Log("Test ERROR", Debug.LogLevel.Error, true);
        Debug.Log("Test FATAL", Debug.LogLevel.FatalError, true);
    }


    private static Scene.Scene? _sceneSnapshot;

    private static void ChangePlayMode(RenderMode renderMode)
    {
        if (renderMode == RenderMode.EditorRun)
        {
            ProjectScriptService.Reload(ProjectFolderPath, throwOnError: true);
            _sceneSnapshot = SceneManager.CloneScene(SceneManager.CurrentScene!);
            _window.ChangePlayMode(RenderMode.EditorRun);

            if (_window.Renderer?._sceneCameras is { Count: > 0 })
            {
                _window.Renderer.ActiveCamera = _window.Renderer._sceneCameras.First();
                Debug.Log("Switched to scene camera", Debug.LogLevel.Info, true);
            }
            else
            {
                Debug.Log("No scene cameras found!", Debug.LogLevel.Warning, true);
            }

            GameLoop.StartLifeCycle();
        }
        else
        {
            _window.ChangePlayMode(RenderMode.EditorStop);

            if (_sceneSnapshot != null)
            {
                SceneManager.RestoreScene(_sceneSnapshot);
                _sceneSnapshot = null;
            }

            if (_window.Renderer?.EditorCamera != null)
            {
                _window.Renderer.ActiveCamera = _window.Renderer.EditorCamera;
                Debug.Log("Switched to editor camera", Debug.LogLevel.Info, true);
            }

            _window.LoadScene?.Invoke();
            HierarchyPanel.OnChangeScene?.Invoke();
        }
    }


    private static void OpenScene(string scenePath)
    {
        ProjectScriptService.Reload(ProjectFolderPath, throwOnError: true);
        SceneManager.LoadSceneByPath(scenePath);
        _window.LoadScene?.Invoke();
        HierarchyPanel.OnChangeScene?.Invoke();
        Debug.Log($"Open scene: {scenePath}", Debug.LogLevel.Info, true);
    }

    public void Dispose()
    {
        SceneManager.AddRenderer -= AddRenderer;
        SceneManager.RemoveRenderer -= RemoveRenderer;

        RendererUI.OnProjectSave -= () => ProjectSettings.SaveProject(_settings);
        ProjectSetiingPanel.OnSaveProjectSettings -= () => ProjectSettings.SaveProjectSettings(_settings);

        ProjectFilePanel.OnSceneOpened -= OpenScene;
        ViewportPanel.OnPlayModeChanged -= ChangePlayMode;

        ProjectFileManager.OnSceneMoved -=
            (oldPath, newPath) => ProjectSettings.UpdateScenePath(_settings, oldPath, newPath);
    }
}