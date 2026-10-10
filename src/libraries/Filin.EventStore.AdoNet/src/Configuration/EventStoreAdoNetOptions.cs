using System.ComponentModel.DataAnnotations;

using Filin.AdoNet;
using Filin.Compliance.Classification;
using Filin.Storage;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Options for Postgres EventStore storage.
/// </summary>
public sealed class EventStoreAdoNetOptions : IStorageProviderSerializerOptions
{
    /// <summary>
    /// Connection string for Postgres EventStore storage.
    /// </summary>
    [ConnectionString]
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Connection name.
    /// </summary>
    [CustomValidation(typeof(EventStoreAdoNetOptions), nameof(IsValidConnectionName))]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// The default ADO.NET invariant used for storage if none is given. 
    /// </summary>
    public const string DefaultAdoNetInvariant = AdoNetInvariants.InvariantNamePostgreSql;

    /// <summary>
    /// The invariant name for storage.
    /// </summary>
    [Required]
    [CustomValidation(typeof(EventStoreAdoNetOptions), nameof(IsValidInvariant))]
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
    /// Appending queue size.
    /// </summary>
    /// <remarks>-1 unlimited capacity. Default is 1000.</remarks>
    [Range(-1, int.MaxValue)]
    public int AppendBoundedCapacity { get; set; } = 1000;

    /// <summary>
    /// <inheritdoc cref="EventStoreAdoNetBackoff" path="/summary"/>
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public required EventStoreAdoNetBackoff Backoff { get; set; } = new EventStoreAdoNetBackoff();

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
    /// <param name="value"><inheritdoc cref="EventStoreAdoNetOptions" path="/summary"/></param>
    /// <param name="context"><inheritdoc cref="ValidationContext" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
    public static ValidationResult? IsValidConnectionName(object value, ValidationContext? context) =>
        (value is not string connectionName || string.IsNullOrWhiteSpace(connectionName))
        && context is not null && context.ObjectInstance is EventStoreAdoNetOptions opt
        && string.IsNullOrWhiteSpace(opt.ConnectionString)
            ? new ValidationResult($"Required '{nameof(ConnectionString)}' or '{nameof(ConnectionName)}'. {typeof(EventStoreAdoNetOptions).FullName}")
            : ValidationResult.Success;

    /// <summary>
    /// Validate <see cref="Invariant"/>.
    /// </summary>
    /// <param name="value"><inheritdoc cref="EventStoreAdoNetOptions" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
    public static ValidationResult? IsValidInvariant(object value) =>
        (value is string invariant) && string.Equals(invariant, AdoNetInvariants.InvariantNamePostgreSql, StringComparison.Ordinal)
        ? ValidationResult.Success
        : new ValidationResult($"Supports '{AdoNetInvariants.InvariantNamePostgreSql}'");
}

/// <summary>
/// Provides compile-time generated validation for <see cref="EventStoreAdoNetOptions"/> using data annotations.
/// </summary>
[OptionsValidator]
public sealed partial class EventStorePostgresOptionsValidator : IValidateOptions<EventStoreAdoNetOptions>
{

}
