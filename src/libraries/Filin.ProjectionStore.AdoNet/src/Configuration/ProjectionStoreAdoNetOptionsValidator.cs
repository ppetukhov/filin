using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Provides compile-time generated validation for <see cref="ProjectionStoreAdoNetOptions"/> using data annotations.
/// </summary>
[OptionsValidator]
public sealed partial class ProjectionStoreAdoNetOptionsValidator : IValidateOptions<ProjectionStoreAdoNetOptions>
{

}
