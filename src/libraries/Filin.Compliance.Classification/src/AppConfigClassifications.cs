using Microsoft.Extensions.Compliance.Classification;

namespace Filin.Compliance.Classification;

/// <summary>
/// Классификатор данных конфигурации.
/// </summary>
public static class AppConfigClassifications
{
    /// <summary>
    /// Taxonomy.
    /// </summary>
    public static string TaxonomyName => "AppConfig";

    /// <summary>
    /// Строка подключения.
    /// </summary>
    public static DataClassification ConnectionString => new(TaxonomyName, nameof(ConnectionString));

    /// <summary>
    /// Логин.
    /// </summary>
    public static DataClassification Login => new(TaxonomyName, nameof(Login));

    /// <summary>
    /// Токен.
    /// </summary>
    public static DataClassification Token => new(TaxonomyName, nameof(Token));

    /// <summary>
    /// Токен авторизации.
    /// </summary>
    public static DataClassification ApiKey => new(TaxonomyName, nameof(ApiKey));

    /// <summary>
    /// Ключ.
    /// </summary>
    public static DataClassification Secret => new(TaxonomyName, nameof(Secret));

    /// <summary>
    /// Учётные данные.
    /// </summary>
    public static DataClassificationSet HasCredentials { get; } = new(ConnectionString, Login, Token, ApiKey, Secret);
}
