using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// Provides data classification constants and taxonomy descriptors for application configuration elements.
/// </summary>
public static class AppConfigClassifications
{
    /// <summary>
    /// Gets the name of the application configuration classification taxonomy.
    /// </summary>
    public static string TaxonomyName => "AppConfig";

    /// <summary>
    /// Gets the classification descriptor for database or service connection strings.
    /// </summary>
    public static DataClassification ConnectionString => new(TaxonomyName, nameof(ConnectionString));

    /// <summary>
    /// Gets the classification descriptor for authentication user logins or usernames.
    /// </summary>
    public static DataClassification Login => new(TaxonomyName, nameof(Login));

    /// <summary>
    /// Gets the classification descriptor for session or identity authentication tokens.
    /// </summary>
    public static DataClassification Token => new(TaxonomyName, nameof(Token));

    /// <summary>
    /// Gets the classification descriptor for external API authorization keys.
    /// </summary>
    public static DataClassification ApiKey => new(TaxonomyName, nameof(ApiKey));

    /// <summary>
    /// Gets the classification descriptor for general cryptographic keys, salts, or passwords.
    /// </summary>
    public static DataClassification Secret => new(TaxonomyName, nameof(Secret));

    /// <summary>
    /// Gets the unified set of classification descriptors that represent sensitive infrastructure credentials.
    /// </summary>
    public static DataClassificationSet HasCredentials { get; } = new(ConnectionString, Login, Token, ApiKey, Secret);
}
