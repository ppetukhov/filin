using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// Login.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.ReturnValue,
    AllowMultiple = true,
    Inherited = false)]
public sealed class LoginAttribute() : DataClassificationAttribute(AppConfigClassifications.Login)
{
}
