using Microsoft.CodeAnalysis;

namespace Filin.Generators;

sealed class TypeDefinitions(Compilation compilation)
{
    /// <inheritdoc cref="Compilation.GlobalNamespace"/>
    public INamespaceSymbol GlobalNamespace { get; } = compilation.GlobalNamespace;
    public INamedTypeSymbol IDecider { get; } = Type(Constants.IDeciderFullName, compilation);
    public INamedTypeSymbol IWithQueryTags { get; } = Type(Constants.IWithQueryTagsFullName, compilation);
    public INamedTypeSymbol IProjector { get; } = Type(Constants.IProjectorFullName, compilation);
    public string AssemblyName { get; } = compilation.AssemblyName ?? string.Empty;

    internal bool IsDecider(INamedTypeSymbol symbol) => SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, IDecider) || symbol.Interfaces.Any(IsDecider);

    internal bool IsProjector(INamedTypeSymbol symbol) => SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, IProjector) || symbol.Interfaces.Any(IsProjector);

    internal INamedTypeSymbol? FindIDecider(INamedTypeSymbol symbol)
    {
        if (SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, IDecider))
        {
            return symbol;
        }

        foreach (var item in symbol.Interfaces)
        {
            if (FindIDecider(item) is not null)
            {
                return item;
            }
        }

        return null;
    }

    internal INamedTypeSymbol? FindIProjector(INamedTypeSymbol symbol)
    {
        if (SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, IProjector))
        {
            return symbol;
        }

        foreach (var item in symbol.Interfaces)
        {
            if (FindIProjector(item) is not null)
            {
                return item;
            }
        }

        return null;
    }

    static INamedTypeSymbol Type(string metadataName, Compilation compilation) =>
        compilation.GetTypeByMetadataName(TrimGlobalPrefix(metadataName))
        ?? throw new InvalidOperationException("Cannot find type with metadata name " + metadataName);

    static string TrimGlobalPrefix(string value) => value.StartsWith("global::", StringComparison.Ordinal) ? value.Substring(8) : value;
}
