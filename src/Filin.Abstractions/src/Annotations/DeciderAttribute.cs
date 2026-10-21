namespace Filin;

/// <summary>
/// Marks a class or struct as a domain command decider (handler).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class DeciderAttribute : Attribute { }
