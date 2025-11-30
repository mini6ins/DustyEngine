namespace DustyEngineEditor.Panels.RemoteRenderer;

public interface IRemoteRenderer
{
    Task<FrameData> GetFrameData(float time);
    void OnKeyDown(string key);
    void OnKeyUp(string key);
    void OnMouseDown(float normalizedX, float normalizedY, int button);
    void OnMouseUp(float normalizedX, float normalizedY, int button);
    void OnMouseMoveDelta(float deltaX, float deltaY);
    void OnMouseClick(float normalizedX, float normalizedY, int button);
}