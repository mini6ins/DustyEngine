using System.Numerics;
using System.Reflection;
using DustyEngine;
using DustyEngine.Components;
using ImGuiNET;
using SceneSystem.Attributes;
using SceneSystem.EngineObject.GameObject;
using Component = DustyEngine.Components.Component;
using NumVec3 = System.Numerics.Vector3;
using DeVec3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace Editor.Panels.InspectorPanel;

internal class InspectorPanel : IRenderablePanel
{
    private static GameObject? _selectedGameObject;

    private static string _addCompSearch = "";
    private static readonly IReadOnlyList<Type> Types = ComponentTypeCache.GetAll();

    public static void SetSelectedGameObject(GameObject selectedGameObject) => _selectedGameObject = selectedGameObject;

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Inspector Panel"))
        {
            ImGui.End();
            return;
        }

        if (_selectedGameObject == null)
        {
            ImGui.TextDisabled("No object selected");
            ImGui.End();
            return;
        }

        DrawGameObjectInfo();
        ImGui.Separator();
        DrawObjectComponents(_selectedGameObject);
        ImGui.Separator();
        DrawAddComponent();

        ImGui.End();
    }


    private static void DrawGameObjectInfo()
    {
        if (_selectedGameObject == null) return;

        var active = _selectedGameObject.ActiveSelf;
        if (ImGui.Checkbox("##enabled", ref active))
            _selectedGameObject.ActiveSelf = active;

        ImGui.SameLine();

        ImGui.SetNextItemWidth(-1);
        var objectName = _selectedGameObject.Name;
        if (ImGui.InputText("##name", ref objectName, 256))
            _selectedGameObject.Name = objectName;
    }


    private static void DrawObjectComponents(GameObject gameObject)
    {
        if (gameObject.Components.Count == 0)
        {
            ImGui.TextDisabled("No components");
            return;
        }

        Component? toRemove = null;

        foreach (var component in gameObject.Components)
        {
            ImGui.PushID((int)component.Id);

            var opened = ImGui.CollapsingHeader(component.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && component.GetType() != typeof(Transform))
                ImGui.OpenPopup("comp_menu");

            if (ImGui.BeginPopup("comp_menu"))
            {
                if (ImGui.MenuItem("Remove"))
                {
                    toRemove = component;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            if (opened)
            {
                DrawComponentFields(component);
                DrawComponentProperties(component);
            }

            ImGui.PopID();
        }

        if (toRemove != null)
            gameObject.RemoveComponent(toRemove.Id);
    }


    private static void DrawComponentProperties(object component)
    {
        var type = component.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead) continue;
            if (prop.GetIndexParameters().Length != 0) continue;

            if (prop.GetCustomAttribute<HideInInspectorAttribute>() != null)
                continue;

            var readOnly = !prop.CanWrite || HasAttribute<ReadOnlyInInspectorAttribute>(prop);

            DrawValue(
                label: prop.Name,
                valueType: prop.PropertyType,
                get: () => prop.GetValue(component),
                set: v => prop.SetValue(component, v),
                canWrite: prop.CanWrite && !readOnly,
                readOnlyUi: readOnly
            );
        }
    }

    private static void DrawComponentFields(object component)
    {
        var type = component.GetType();

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!ShouldDraw(field))
                continue;

            DrawValue(
                label: field.Name,
                valueType: field.FieldType,
                get: () => field.GetValue(component),
                set: v => field.SetValue(component, v),
                canWrite: field is { IsInitOnly: false, IsLiteral: false }
            );
        }
    }


    private static void DrawValue(string label, Type valueType, Func<object?> get, Action<object?> set, bool canWrite,
        bool readOnlyUi = false)
    {
        if (readOnlyUi) ImGui.BeginDisabled();

        var value = get();

        if (value == null)
        {
            ImGui.Text(label);
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

            if (valueType == typeof(string))
            {
                var str = "";
                if (ImGui.InputText($"##{label}", ref str, 256) && canWrite)
                    set(str);
            }
            else if (valueType == typeof(Mesh))
            {
                ImGui.TextDisabled("(None)");
            }
            else
            {
                ImGui.TextDisabled("(null)");
            }

            if (readOnlyUi) ImGui.EndDisabled();
            return;
        }


        ImGui.Text(label);
        ImGui.SameLine(120);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

        switch (value)
        {
            case int i:
                if (ImGui.DragInt($"##{label}", ref i) && canWrite) set(i);
                break;

            case float f:
                if (ImGui.DragFloat($"##{label}", ref f, 0.1f) && canWrite) set(f);
                break;

            case bool b:
                if (ImGui.Checkbox($"##{label}", ref b) && canWrite) set(b);
                break;

            case DeVec3 dv:
            {
                var tmp = new NumVec3(dv.X, dv.Y, dv.Z);
                if (ImGui.DragFloat3($"##{label}", ref tmp, 0.1f) && canWrite)
                {
                    dv.X = tmp.X;
                    dv.Y = tmp.Y;
                    dv.Z = tmp.Z;
                    set(dv);
                }

                break;
            }

            case string s:
            {
                var str = s ?? "";
                if (ImGui.InputText($"##{label}", ref str, 256) && canWrite)
                    set(str);
                break;
            }
            case Mesh mesh:
            {
                ImGui.TextDisabled($"Mesh: {mesh}");
                break;
            }


            case null when valueType == typeof(string):
            {
                var str = "";
                if (ImGui.InputText($"##{label}", ref str, 256) && canWrite)
                    set(str);
                break;
            }

            default:
                ImGui.TextDisabled($"({valueType.Name})");
                break;
        }

        if (readOnlyUi) ImGui.EndDisabled();
    }


    private static void DrawAddComponent()
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - 200f) * 0.5f);

        if (ImGui.Button("Add Component", new Vector2(200f, 25f)))
            ImGui.OpenPopup("add_component_popup");


        var popupPos = ImGui.GetCursorScreenPos();
        ImGui.SetNextWindowPos(popupPos, ImGuiCond.Always);

        ImGui.SetNextWindowSize(new Vector2(340, 420), ImGuiCond.Always);

        if (!ImGui.BeginPopup("add_component_popup")) return;

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Type to search...", ref _addCompSearch, 128);
        ImGui.PopItemWidth();

        ImGui.Separator();

        if (ImGui.BeginChild("##list", new Vector2(0, 0)))
        {
            var result = Types
                .Where(t => t.Name.Contains(_addCompSearch, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => GetScore(t.Name, _addCompSearch))
                .ThenBy(t => t.Name)
                .ToList();

            foreach (var item in result.Where(item => ImGui.Selectable(item.Name, false)))
            {
                if (Activator.CreateInstance(item) is Component comp)
                {
                    _selectedGameObject?.AddComponent(comp);
                }
                else
                {
                    Debug.Log($"Can't create component: {item.FullName}", Debug.LogLevel.Error, true);
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndChild();
        }

        ImGui.EndPopup();
    }


    private static int GetScore(string text, string query)
    {
        text = text.ToLower();
        query = query.ToLower();

        var score = 0;

        if (text == query) score -= 100;
        if (text.StartsWith(query)) score -= 50;
        if (text.Contains(query)) score -= 20;

        foreach (var c in query.Where(c => text.Contains(c))) score--;

        return score;
    }


    private static bool HasAttribute<T>(MemberInfo member) where T : Attribute =>
        Attribute.IsDefined(member, typeof(T));


    private static bool ShouldDraw(FieldInfo field)
    {
        var hasSerialize = HasAttribute<SerializeFieldAttribute>(field);
        var hasHide = HasAttribute<HideInInspectorAttribute>(field);

        if (hasHide) return false;

        return field.IsPublic || hasSerialize;
    }
}
