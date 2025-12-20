using System.Numerics;
using ImGuiNET;

namespace GraphicsEngineOpenGL.Editor.Panels.ViewPortPanel.Themes;

public enum EditorTheme
{
    Dark,
    Light,
    LegacyClassic,
    Gruvbox,
}

public static class ThemeSelector
{
    public static EditorTheme CurrentTheme = EditorTheme.Dark;

    public static void ApplyTheme(EditorTheme theme)
    {
        CurrentTheme = theme;

        switch (theme)
        {
            case EditorTheme.Dark:
                ImGui.StyleColorsDark();
                break;

            case EditorTheme.Light:
                ImGui.StyleColorsLight();
                break;

            case EditorTheme.LegacyClassic:
                ImGui.StyleColorsClassic();
                break;

            case EditorTheme.Gruvbox:
                ApplyGruvbox();
                break;
        }
    }

    private static void ApplyGruvbox()
    {
        var style = ImGui.GetStyle();
        var colors = style.Colors;

// ===== Backgrounds =====
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.157f, 0.157f, 0.137f, 1.0f); // #282828
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.157f, 0.157f, 0.137f, 1.0f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.196f, 0.188f, 0.169f, 1.0f); // #32302f

// ===== Borders =====
        colors[(int)ImGuiCol.Border] = new Vector4(0.298f, 0.275f, 0.259f, 1.0f); // #3c3836
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f);

// ===== Text =====
        colors[(int)ImGuiCol.Text] = new Vector4(0.922f, 0.859f, 0.698f, 1.0f); // #ebdbb2
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.573f, 0.514f, 0.451f, 1.0f); // #928374

// ===== Headers (TreeNode, Selectable) =====
        colors[(int)ImGuiCol.Header] = new Vector4(0.196f, 0.188f, 0.169f, 1.0f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.843f, 0.600f, 0.129f, 1.0f); // #d79921
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.992f, 0.502f, 0.098f, 1.0f); // #fe8019

// ===== Buttons =====
        colors[(int)ImGuiCol.Button] = new Vector4(0.196f, 0.188f, 0.169f, 1.0f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.843f, 0.600f, 0.129f, 1.0f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.992f, 0.502f, 0.098f, 1.0f);

// ===== Frames (InputText, Combo, Slider) =====
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.196f, 0.188f, 0.169f, 1.0f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.298f, 0.275f, 0.259f, 1.0f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.843f, 0.600f, 0.129f, 1.0f);

// ===== Tabs =====
        colors[(int)ImGuiCol.Tab] = new Vector4(0.196f, 0.188f, 0.169f, 1.0f);

// ===== Scrollbar =====ы
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.157f, 0.157f, 0.137f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.298f, 0.275f, 0.259f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered]
            = new Vector4(0.843f, 0.600f, 0.129f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrabActive]
            = new Vector4(0.992f, 0.502f, 0.098f, 1.0f);

// ===== Style rounding =====
        style.WindowRounding = 6f;
        style.FrameRounding = 4f;
        style.ScrollbarRounding = 6f;
        style.TabRounding = 4f;
    }
}
