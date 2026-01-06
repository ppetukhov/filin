namespace Filin;

/// <summary>
/// Тэг события.
/// </summary>
/// <param name="tag">Значение тэга.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
public sealed class EventTagAttribute(string tag) : Attribute
{
    /// <summary>
    /// Значение тэга.
    /// </summary>
    public string Tag { get; } = tag;
}
