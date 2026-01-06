using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Filin.Generators;

sealed class TypeDefinitions(Compilation Compilation)
{
    /// <inheritdoc cref="Compilation.GlobalNamespace"/>
    public INamespaceSymbol GlobalNamespace { get; } = Compilation.GlobalNamespace;
    public INamedTypeSymbol ApplicationPartAttribute { get; } = Type(Constants.ApplicationPartAttribute, Compilation);
    public INamedTypeSymbol IDecider { get; } = Type(Constants.IDecider, Compilation);
    public INamedTypeSymbol IWithQueryTags { get; } = Type(Constants.IWithQueryTags, Compilation);
    public INamedTypeSymbol IProjector { get; } = Type(Constants.IProjector, Compilation);
    public ImmutableHashSet<string> ApplicationParts { get; } = CreateApplicationParts(Compilation);
    public string AssemblyName { get; } = Compilation.AssemblyName ?? string.Empty;

    internal bool IsDecider(INamedTypeSymbol symbol) => !SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, IDecider) ? symbol.Interfaces.Any(IsDecider) : true;

    internal bool IsProjector(INamedTypeSymbol symbol) => !SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, IProjector) ? symbol.Interfaces.Any(IsProjector) : true;

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

    static ImmutableHashSet<string> CreateApplicationParts(Compilation compilation)
    {
        var applicationPartAttribute = Type(Constants.ApplicationPartAttribute, compilation);

        var builder = ImmutableHashSet.CreateBuilder(StringComparer.Ordinal);

        var referencedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default)
        {
            // Expand the set of referenced assemblies
            compilation.Assembly
        };

        builder.Add(compilation.Assembly.MetadataName);

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm)
            {
                continue;
            }

            if (!referencedAssemblies.Add(asm))
            {
                continue;
            }

            var added = false;

            foreach (var attr in asm.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(applicationPartAttribute, attr.AttributeClass))
                {
                    if (!added)
                    {
                        builder.Add(asm.MetadataName);
                        added = true;
                    }

                    builder.Add((string)attr.ConstructorArguments.First().Value!);
                }
            }
        }

        return builder.ToImmutable();
    }
}
