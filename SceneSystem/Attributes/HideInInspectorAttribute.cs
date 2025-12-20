namespace SceneSystem.Attributes;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class HideInInspectorAttribute : Attribute {}
