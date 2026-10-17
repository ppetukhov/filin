using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Provides validation for <see cref="ProjectionStoreAdoNetOptions.ConnectionName"/>.
/// </summary>
sealed class ProjectionStoreAdoNetOptionsConnectionNameValidator(IConfiguration configuration) : IValidateOptions<ProjectionStoreAdoNetOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ProjectionStoreAdoNetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.ConnectionName))
        {
            return ValidateOptionsResult.Success;
        }

        var connectionString = configuration.GetConnectionString(options.ConnectionName);

        return connectionString is null
            ? ValidateOptionsResult.Fail($"{nameof(options.ConnectionName)}: The connection string specified by {(string.IsNullOrEmpty(name) ? nameof(ProjectionStoreAdoNetOptions) : name)}.{nameof(options.ConnectionName)} ('{options.ConnectionName}') not found")
            : ValidateOptionsResult.Success;
    }
}
