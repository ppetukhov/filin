using System.ComponentModel.DataAnnotations;
using System.Data.Common;

using Filin.AdoNet;
using Filin.Storage;

namespace Filin.Configuration;

/// <summary>
/// Options for Postgres EventStore storage.
/// </summary>
public sealed class EventStoreAdoNetOptions : IValidatableObject, IStorageProviderSerializerOptions
{
    /// <summary>
    /// Gets config section name.
    /// </summary>
    public const string SectionName = "Filin:EventStore";

    /// <summary>
    /// The default ADO.NET invariant used for storage if none is given. 
    /// </summary>
    public const string DefaultAdoNetInvariant = AdoNetInvariants.InvariantNamePostgreSql;

    /// <summary>
    /// Connection name.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Connection string for Postgres EventStore storage.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The invariant name for storage.
    /// </summary>
    [Required]
    [CustomValidation(typeof(EventStoreAdoNetOptions), nameof(IsValidInvariant))]
    public string Invariant { get; set; } = DefaultAdoNetInvariant;

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
    public required EventStoreAdoNetBackoff Backoff { get; set; } = new EventStoreAdoNetBackoff();

    /// <summary>
    /// Gets event types.
    /// </summary>
    public Dictionary<EventType, Type> EventTypes { get; } = [];

    /// <inheritdoc/>
    [Required]
    public required IStorageSerializer StorageSerializer { get; set; }

    /// <summary>
    /// Gets Data source.
    /// </summary>
    [Required]
    public required DbDataSource DataSource { get; set; }

    /// <summary>
    /// Validate <see cref="Invariant"/>.
    /// </summary>
    /// <param name="value"><inheritdoc cref="EventStoreAdoNetOptions" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
    public static ValidationResult? IsValidInvariant(object value) =>
        (value is string invariant) && string.Equals(invariant, AdoNetInvariants.InvariantNamePostgreSql, StringComparison.Ordinal)
        ? ValidationResult.Success
        : new ValidationResult($"{nameof(Invariant)}: The value ('{value}') is not supported for {nameof(EventStoreAdoNetOptions)}.{nameof(Invariant)}. Allowed values: '{AdoNetInvariants.InvariantNamePostgreSql}'");

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(ConnectionString) && string.IsNullOrEmpty(ConnectionName) && (DataSource is null))
        {
            yield return new ValidationResult($"{nameof(EventStoreAdoNetOptions)}: Required '{nameof(ConnectionString)}' or '{nameof(ConnectionName)}' or '{nameof(DataSource)}'");
        }

        if (!string.IsNullOrEmpty(Invariant)
            && AdoNetInvariants.Invariants.Contains(Invariant)
            && DataSource is not null
            && !DbConnectionFactory.IsValidDataSource(Invariant, DataSource, out var errorMessage))
        {
            yield return new ValidationResult(errorMessage);
        }
    }
}
