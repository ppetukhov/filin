using System.ComponentModel.DataAnnotations;
using System.Data.Common;

using Filin.AdoNet;
using Filin.Storage;

namespace Filin.Configuration;

/// <summary>
/// Options for ADO.NET ProjectionStore storage.
/// </summary>
public sealed class ProjectionStoreAdoNetOptions : IValidatableObject
{
    /// <summary>
    /// Gets config section name.
    /// </summary>
    public const string SectionName = "Filin:ProjectionStore";

    /// <summary>
    /// The default ADO.NET invariant used for storage if none is given. 
    /// </summary>
    public const string DefaultAdoNetInvariant = AdoNetInvariants.InvariantNamePostgreSql;

    /// <summary>
    /// Connection name.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Connection string for ADO.NET ProjectionStore storage.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The invariant name for storage.
    /// </summary>
    [Required]
    [CustomValidation(typeof(ProjectionStoreAdoNetOptions), nameof(IsValidInvariant))]
    public string Invariant { get; set; } = DefaultAdoNetInvariant;

    /// <summary>
    /// The serializer identity.
    /// </summary>
    public string? SerializerKey { get; set; }

    /// <summary>
    /// Gets projection types.
    /// </summary>
    public Dictionary<ProjectorId, Type> ProjectionTypes { get; } = [];

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
    /// <param name="value"><inheritdoc cref="ProjectionStoreAdoNetOptions" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
    public static ValidationResult? IsValidInvariant(object value) =>
        (value is string invariant) && AdoNetInvariants.Invariants.Contains(invariant, StringComparer.Ordinal)
        ? ValidationResult.Success
        : new ValidationResult($"{nameof(Invariant)}: The value ('{value}') is not supported for {nameof(ProjectionStoreAdoNetOptions)}.{nameof(Invariant)}. Allowed values: '{string.Join("', '", AdoNetInvariants.Invariants)}'");

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(ConnectionString) && string.IsNullOrEmpty(ConnectionName) && (DataSource is null))
        {
            yield return new ValidationResult($"{nameof(ProjectionStoreAdoNetOptions)}: Required '{nameof(ConnectionString)}' or '{nameof(ConnectionName)}' or '{nameof(DataSource)}'");
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
