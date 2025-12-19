using System.Runtime.CompilerServices;
using DustyEngine;
using DustyEngine.Scene;
using GraphicsEngineOpenGL;

namespace DustyEngineEditor.Panels.ViewPortPanel.RemoteRenderer;

public interface IRemoteRenderer
{
    Task<FrameData> GetFrameData(float time);
    void OnKeyDown(string key);
    void OnKeyUp(string key);
    void OnMouseDown(float normalizedX, float normalizedY, int button);
    void OnMouseUp(float normalizedX, float normalizedY, int button);
    void OnMouseMoveDelta(float deltaX, float deltaY);
    void OnMouseClick(float normalizedX, float normalizedY, int button);

    void PlayEngine();
    void StopEngine();


    void LogMessage(object? message, Debug.LogLevel level, bool isDebug, string source,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
    );

    Task<Scene?> GetCurrentScene();
}
