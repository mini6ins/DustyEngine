using System.Numerics;
using System.Reflection;
using System.Text;
using DustyEngine;
using DustyEngine.Components;
using ImGuiNET;
using SceneSystem.Attributes;
using SceneSystem.EngineObject.GameObject;
using NumVec3 = System.Numerics.Vector3;
using DeVec3 = DustyEngine.Engine.Math.Vectors.Vector3;

namespace Editor.Panels.InspectorPanel;

internal class InspectorPanel : IRenderablePanel
{
    private static GameObject? _selectedGameObject;

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

    private static string _addCompSearch = "";
    private static IReadOnlyList<Type> _types = ComponentTypeCache.GetAll();


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
            foreach (var item in _types)
            {
                if (!ImGui.Selectable(item.Name, false)) continue;

                if (Activator.CreateInstance(item) is Component comp)
                {
                    _selectedGameObject.AddComponent(comp);
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


    private static void DrawGameObjectInfo()
    {
        if (_selectedGameObject == null) return;

        var active = _selectedGameObject.ActiveSelf;
        if (ImGui.Checkbox("##enabled", ref active))
            _selectedGameObject.ActiveSelf = active;

        ImGui.SameLine();

        var objectName = _selectedGameObject.Name;
        if (ImGui.InputText("##name", ref objectName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
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
            ImGui.PushID(component.GetHashCode());

            var opened = ImGui.CollapsingHeader(component.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
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


    private static bool IsReadOnly(PropertyInfo prop) =>
        !prop.CanWrite || HasAttrByFullName(prop, typeof(ReadOnlyInInspectorAttribute));

    private static bool HasAttrByFullName(MemberInfo member, Type attrType) => attrType.FullName != null &&
                                                                               member.CustomAttributes.Any(cad =>
                                                                                   cad.AttributeType.FullName ==
                                                                                   attrType.FullName);

    private static bool ShouldDraw(FieldInfo field)
    {
        var isPublic = field.IsPublic;

        var hasSerialize = HasAttrByFullName(field, typeof(SerializeFieldAttribute));
        var hasHide = HasAttrByFullName(field, typeof(HideInInspectorAttribute));

        if (hasHide) return false;

        return isPublic || hasSerialize;
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

            DrawProperty(component, prop);
        }
    }

    private static void DrawProperty(object target, PropertyInfo prop)
    {
        var readOnly = IsReadOnly(prop);
        if (readOnly) ImGui.BeginDisabled();

        var label = prop.Name;
        var value = prop.GetValue(target);

        switch (value)
        {
            case int i:
            {
                if (ImGui.DragInt(label, ref i) && prop.CanWrite)
                    prop.SetValue(target, i);

                if (readOnly) ImGui.EndDisabled();
                return;
            }
            case float f:
            {
                if (ImGui.DragFloat(label, ref f, 0.1f) && prop.CanWrite)
                    prop.SetValue(target, f);

                if (readOnly) ImGui.EndDisabled();
                return;
            }
            case bool b:
            {
                if (ImGui.Checkbox(label, ref b) && prop.CanWrite)
                    prop.SetValue(target, b);

                if (readOnly) ImGui.EndDisabled();
                return;
            }
            case DeVec3 dv:
            {
                var tmp = new NumVec3(dv.X, dv.Y, dv.Z);

                if (ImGui.DragFloat3(label, ref tmp, 0.1f))
                {
                    dv.X = tmp.X;
                    dv.Y = tmp.Y;
                    dv.Z = tmp.Z;
                    if (prop.CanWrite) prop.SetValue(target, dv);
                }

                if (readOnly) ImGui.EndDisabled();
                return;
            }
            case string s:
            {
                var buffer = new byte[256];
                Encoding.UTF8.GetBytes(s, buffer);

                if (ImGui.InputText(label, buffer, (uint)buffer.Length) && prop.CanWrite)
                    prop.SetValue(target, Encoding.UTF8.GetString(buffer).TrimEnd('\0'));

                if (readOnly) ImGui.EndDisabled();
                return;
            }
        }

        ImGui.TextDisabled($"{label} ({prop.PropertyType.Name})");

        if (readOnly) ImGui.EndDisabled();
    }

    private static void DrawComponentFields(object component)
    {
        var type = component.GetType();

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!ShouldDraw(field))
                continue;

            DrawField(component, field);
        }
    }


    private static void DrawField(object target, FieldInfo field)
    {
        var label = field.Name;
        var value = field.GetValue(target);

        switch (value)
        {
            case int i:
            {
                if (ImGui.DragInt(label, ref i))
                    field.SetValue(target, i);
                break;
            }
            case float f:
            {
                if (ImGui.DragFloat(label, ref f, 0.1f))
                    field.SetValue(target, f);
                break;
            }
            case bool b:
            {
                if (ImGui.Checkbox(label, ref b))
                    field.SetValue(target, b);
                break;
            }
            case DeVec3 dv:
            {
                var tmp = new NumVec3(dv.X, dv.Y, dv.Z);
                if (ImGui.DragFloat3(label, ref tmp, 0.1f))
                {
                    dv.X = tmp.X;
                    dv.Y = tmp.Y;
                    dv.Z = tmp.Z;
                    field.SetValue(target, dv);
                }

                break;
            }
            case string s:
            {
                var buffer = new byte[256];
                Encoding.UTF8.GetBytes(s, buffer);

                if (ImGui.InputText(label, buffer, (uint)buffer.Length))
                    field.SetValue(target, Encoding.UTF8.GetString(buffer).TrimEnd('\0'));
                break;
            }
            default:
                ImGui.TextDisabled($"{label} ({field.FieldType.Name})");
                break;
        }
    }
}
