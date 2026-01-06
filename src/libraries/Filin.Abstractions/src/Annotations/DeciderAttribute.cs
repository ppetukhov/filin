namespace Filin;

/// <summary>
/// Исполнитель команды домена.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class DeciderAttribute : Attribute { }
