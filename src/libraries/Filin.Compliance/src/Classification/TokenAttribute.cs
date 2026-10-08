using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// Token.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.ReturnValue,
    AllowMultiple = true,
    Inherited = false)]
public sealed class TokenAttribute() : DataClassificationAttribute(AppConfigClassifications.Token)
{
}
