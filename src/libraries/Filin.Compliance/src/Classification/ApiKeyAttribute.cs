using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// ApiKey.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.ReturnValue,
    AllowMultiple = true,
    Inherited = false)]
public sealed class ApiKeyAttribute() : DataClassificationAttribute(AppConfigClassifications.ApiKey)
{
}
