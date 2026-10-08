using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// ConnectionString.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.ReturnValue,
    AllowMultiple = true,
    Inherited = false)]
public sealed class ConnectionStringAttribute() : DataClassificationAttribute(AppConfigClassifications.ConnectionString)
{
}
