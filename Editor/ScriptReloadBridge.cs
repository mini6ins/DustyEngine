namespace Editor;

public static class ScriptReloadBridge
{
    public static volatile bool PendingReload = false;
    public static Action? OnReload;
}