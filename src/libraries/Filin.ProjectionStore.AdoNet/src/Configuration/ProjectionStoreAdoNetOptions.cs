using System.ComponentModel.DataAnnotations;

using Filin.AdoNet;
using Filin.Storage;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Options for ADO.NET ProjectionStore storage.
/// </summary>
public sealed class ProjectionStoreAdoNetOptions : IStorageProviderSerializerOptions
{
    /// <summary>
    /// Connection string for ADO.NET ProjectionStore storage.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Connection name.
    /// </summary>
    [CustomValidation(typeof(ProjectionStoreAdoNetOptions), nameof(IsValidConnectionName))]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// The default ADO.NET invariant used for storage if none is given. 
    /// </summary>
    public const string DefaultAdoNetInvariant = AdoNetInvariants.InvariantNamePostgreSql;

    /// <summary>
    /// The invariant name for storage.
    /// </summary>
    [Required]
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
    /// Validate <see cref="ConnectionName"/>.
    /// </summary>
    /// <param name="value"><inheritdoc cref="ProjectionStoreAdoNetOptions" path="/summary"/></param>
    /// <param name="context"><inheritdoc cref="ValidationContext" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
    public static ValidationResult? IsValidConnectionName(object value, ValidationContext? context) =>
        (value is not string connectionName || string.IsNullOrWhiteSpace(connectionName))
        && context is not null && context.ObjectInstance is ProjectionStoreAdoNetOptions opt
        && string.IsNullOrWhiteSpace(opt.ConnectionString)
            ? new ValidationResult($"Required '{nameof(ConnectionString)}' or '{nameof(ConnectionName)}'. {typeof(ProjectionStoreAdoNetOptions).FullName}")
            : ValidationResult.Success;
}

/// <summary>
/// Provides compile-time generated validation for <see cref="ProjectionStoreAdoNetOptions"/> using data annotations.
/// </summary>
[OptionsValidator]
public sealed partial class ProjectionStoreAdoNetOptionsValidator : IValidateOptions<ProjectionStoreAdoNetOptions>
{

}
