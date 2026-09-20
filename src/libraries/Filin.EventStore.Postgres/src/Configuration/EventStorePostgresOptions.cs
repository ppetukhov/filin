using System.ComponentModel.DataAnnotations;

using Filin.AdoNet;
using Filin.Compliance.Classification;
using Filin.Storage;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Options for Postgres EventStore storage.
/// </summary>
public sealed class EventStorePostgresOptions : IStorageProviderSerializerOptions
{
    /// <summary>
    /// Connection string for Postgres EventStore storage.
    /// </summary>
    [ConnectionString]
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Connection name.
    /// </summary>
    [CustomValidation(typeof(EventStorePostgresOptions), nameof(IsValidConnectionName))]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// The default ADO.NET invariant used for storage if none is given. 
    /// </summary>
    public const string DefaultAdoNetInvariant = AdoNetInvariants.InvariantNamePostgreSql;

    /// <summary>
    /// The invariant name for storage.
    /// </summary>
    [Required]
    public string Invariant { get; } = DefaultAdoNetInvariant;

    /// <summary>
    /// The serializer identity.
    /// </summary>
    public string? SerializerKey { get; set; }

    /// <summary>
    /// Reading batch size.
    /// </summary>
    /// <remarks>Default is 1000.</remarks>
    [Range(1, 10_000)]
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Subscription event buffer.
    /// </summary>
    /// <remarks>Default is 1000.</remarks>
    [Range(1, 10_000)]
    public int SubscriptionEventBuffer { get; set; } = 1000;

    /// <summary>
    /// <inheritdoc cref="EventStorePostgresBackoff" path="/summary"/>
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public required EventStorePostgresBackoff Backoff { get; set; } = new EventStorePostgresBackoff();

    /// <summary>
    /// Gets event types.
    /// </summary>
    public Dictionary<EventType, Type> EventTypes { get; } = [];

    /// <inheritdoc/>
    [Required]
    public required IStorageSerializer StorageSerializer { get; set; }

    /// <summary>
    /// Validate <see cref="ConnectionName"/>.
    /// </summary>
    /// <param name="value"><inheritdoc cref="EventStorePostgresOptions" path="/summary"/></param>
    /// <param name="context"><inheritdoc cref="ValidationContext" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
    public static ValidationResult? IsValidConnectionName(object value, ValidationContext context) =>
        (value is not string connectionName || string.IsNullOrWhiteSpace(connectionName))
        && context.ObjectInstance is EventStorePostgresOptions opt
        && string.IsNullOrWhiteSpace(opt.ConnectionString)
            ? new ValidationResult($"Required '{nameof(ConnectionString)}' or '{nameof(ConnectionName)}'. {typeof(EventStorePostgresOptions).FullName}")
            : ValidationResult.Success;
}

[OptionsValidator]
public sealed partial class EventStorePostgresOptionsValidator : IValidateOptions<EventStorePostgresOptions>
{

}
