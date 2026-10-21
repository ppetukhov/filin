namespace Filin;

/// <summary>
/// Associates a metadata tag with an event, property, or parameter.
/// </summary>
/// <param name="tag">The string value of the tag.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
public sealed class EventTagAttribute(string tag) : Attribute
{
    /// <summary>
    /// Gets the tag value.
    /// </summary>
    public string Tag => tag;
}
