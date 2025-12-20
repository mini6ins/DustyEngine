namespace SceneSystem.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class SerializeFieldAttribute : Attribute
{
}
