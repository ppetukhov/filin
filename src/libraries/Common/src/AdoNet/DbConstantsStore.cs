namespace Filin.AdoNet;

static class DbConstantsStore
{
    static readonly Dictionary<string, DbConstants> InvariantNameToConsts =
        new()
        {
            {
                AdoNetInvariants.InvariantNameSqlServer,
                new DbConstants(startEscapeIndicator: '[',
                                endEscapeIndicator: ']',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousAdoNetImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true)
            },
            {AdoNetInvariants.InvariantNameMySql, new DbConstants(
                                startEscapeIndicator: '`',
                                endEscapeIndicator: '`',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousAdoNetImplementation: true,
                                supportsStreamNatively: false,
                                supportsCommandCancellation: false)
            },
            {AdoNetInvariants.InvariantNamePostgreSql, new DbConstants(
                                startEscapeIndicator: '"',
                                endEscapeIndicator: '"',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousAdoNetImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true)

            },
            {
                AdoNetInvariants.InvariantNameSqlServerDotnetCore,
                new DbConstants(startEscapeIndicator: '[',
                                endEscapeIndicator: ']',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousAdoNetImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true)
            },
            {
                AdoNetInvariants.InvariantNameMySqlConnector,
                new DbConstants(startEscapeIndicator: '[',
                                endEscapeIndicator: ']',
                                unionAllSelectTemplate: " UNION ALL SELECT ",
                                isSynchronousAdoNetImplementation: false,
                                supportsStreamNatively: true,
                                supportsCommandCancellation: true)
            }
        };

    public static DbConstants GetDbConstants(string invariantName) => InvariantNameToConsts[invariantName];

    /// <summary>
    /// If the underlying storage supports cancellation or not.
    /// </summary>
    /// <param name="storage">The storage used.</param>
    /// <returns><em>TRUE</em> if cancellation is supported. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsCommandCancellation(this IRelationalStorage storage) => SupportsCommandCancellation(storage.InvariantName);

    /// <summary>
    /// If the provider supports cancellation or not.
    /// </summary>
    /// <param name="adoNetProvider">The ADO.NET provider invariant string.</param>
    /// <returns><em>TRUE</em> if cancellation is supported. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsCommandCancellation(string adoNetProvider) => GetDbConstants(adoNetProvider).SupportsCommandCancellation;

    /// <summary>
    /// If the underlying storage supports streaming natively.
    /// </summary>
    /// <param name="storage">The storage used.</param>
    /// <returns><em>TRUE</em> if streaming is supported natively. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsStreamNatively(this IRelationalStorage storage) => SupportsStreamNatively(storage.InvariantName);

    /// <summary>
    /// If the provider supports streaming natively.
    /// </summary>
    /// <param name="adoNetProvider">The ADO.NET provider invariant string.</param>
    /// <returns><em>TRUE</em> if streaming is supported natively. <em>FALSE</em> otherwise.</returns>
    public static bool SupportsStreamNatively(string adoNetProvider) => GetDbConstants(adoNetProvider).SupportsStreamNatively;

    /// <summary>
    /// If the underlying ADO.NET implementation is known to be synchronous.
    /// </summary>
    /// <param name="storage">The storage used.</param>
    /// <returns></returns>
    public static bool IsSynchronousAdoNetImplementation(this IRelationalStorage storage) =>
        //Currently the assumption is all but MySQL are asynchronous.
        IsSynchronousAdoNetImplementation(storage.InvariantName);

    /// <summary>
    /// If the provider supports cancellation or not.
    /// </summary>
    /// <param name="adoNetProvider">The ADO.NET provider invariant string.</param>
    /// <returns></returns>
    public static bool IsSynchronousAdoNetImplementation(string adoNetProvider) => GetDbConstants(adoNetProvider).IsSynchronousAdoNetImplementation;
}

sealed class DbConstants(
    char startEscapeIndicator,
    char endEscapeIndicator,
    string unionAllSelectTemplate,
    bool isSynchronousAdoNetImplementation,
    bool supportsStreamNatively,
    bool supportsCommandCancellation)
{
    /// <summary>
    /// A query template for union all select
    /// </summary>
    public readonly string UnionAllSelectTemplate = unionAllSelectTemplate;

    /// <summary>
    /// Indicates whether the ADO.net provider does only support synchronous operations.
    /// </summary>
    public readonly bool IsSynchronousAdoNetImplementation = isSynchronousAdoNetImplementation;

    /// <summary>
    /// Indicates whether the ADO.net provider does streaming operations natively.
    /// </summary>
    public readonly bool SupportsStreamNatively = supportsStreamNatively;

    /// <summary>
    /// Indicates whether the ADO.net provider supports cancellation of commands.
    /// </summary>
    public readonly bool SupportsCommandCancellation = supportsCommandCancellation;

    /// <summary>
    /// The character that indicates a start escape key for columns and tables that are reserved words.
    /// </summary>
    public readonly char StartEscapeIndicator = startEscapeIndicator;

    /// <summary>
    /// The character that indicates an end escape key for columns and tables that are reserved words.
    /// </summary>
    public readonly char EndEscapeIndicator = endEscapeIndicator;
}
