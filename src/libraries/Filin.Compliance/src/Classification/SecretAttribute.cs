using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// Secret.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.ReturnValue,
    AllowMultiple = true,
    Inherited = false)]
public sealed class SecretAttribute() : DataClassificationAttribute(AppConfigClassifications.Secret)
{
}
