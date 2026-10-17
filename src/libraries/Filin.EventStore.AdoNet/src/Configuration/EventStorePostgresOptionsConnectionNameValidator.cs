using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Provides validation for <see cref="EventStoreAdoNetOptions.ConnectionName"/>.
/// </summary>
public sealed class EventStorePostgresOptionsConnectionNameValidator(IConfiguration configuration) : IValidateOptions<EventStoreAdoNetOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, EventStoreAdoNetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.ConnectionName))
        {
            return ValidateOptionsResult.Success;
        }

        var connectionString = configuration.GetConnectionString(options.ConnectionName);

        return connectionString is null
            ? ValidateOptionsResult.Fail($"{nameof(options.ConnectionName)}: The connection string specified by {(string.IsNullOrEmpty(name) ? nameof(EventStoreAdoNetOptions) : name)}.{nameof(options.ConnectionName)} ('{options.ConnectionName}') not found")
            : ValidateOptionsResult.Success;
    }
}
