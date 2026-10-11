using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Filin.Compliance.Classification;
using Filin.Compliance.Redaction;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Filin.Hosting;

/// <summary>
/// Provides extension methods for registering default infrastructure services to <see cref="IFilinBuilder"/>.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "False positive for the new extension syntax.")]
public static class FilinBuilderDefaultServicesExtensions
{
    extension(IFilinBuilder builder)
    {
        /// <summary>
        /// Adds the default core and infrastructure services required for the Filin framework to function.
        /// </summary>
        /// <returns>The same <see cref="IFilinBuilder"/> instance so that multiple calls can be chained.</returns>
        [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.ApplyConfiguration(IFilinBuilder)")]
        [RequiresUnreferencedCode("Calls Filin.Hosting.SerializationServiceCollectionExtentions.AddStorageSerialization()")]
        [RequiresDynamicCode("Calls Filin.Hosting.SerializationServiceCollectionExtentions.AddStorageSerialization()")]
        public IFilinBuilder AddDefaultServices()
        {
            ArgumentNullException.ThrowIfNull(builder);

            var services = builder.Services
                .AddOptions()
                .AddLogging(builder => builder.EnableRedaction())
                .AddRedaction(builder => builder.SetRedactor<ConnectionStringRedactor>(AppConfigClassifications.ConnectionString))
                .AddStorageSerialization();

            services.TryAddSingleton(TimeProvider.System);

            var relevantAssembles = new RelevantAssemblies([.. ReferencedAssemblyProvider.GetRelevantAssemblies()]);

            foreach (var assembly in relevantAssembles.Assembles)
            {
                _ = builder.AddAssembly(assembly);
            }

            ApplyConfiguration(builder, relevantAssembles);

            return builder;
        }
    }

    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.GetRegisteredProviders()")]
    static void ApplyConfiguration(IFilinBuilder builder, RelevantAssemblies relevantAssemblies)
    {
        var cfg = builder.Configuration.GetSection("Filin");
        var knownProviderTypes = GetRegisteredProviders(relevantAssemblies);

        if (bool.TryParse(cfg["EnableDistributedTracing"], out var enableDistributedTracing) && enableDistributedTracing)
        {
            builder.Services.TryAddSingleton(DistributedContextPropagator.Current);
        }

        ApplySubsection(builder, cfg, knownProviderTypes, "EventStore");
        ApplyNamedSubsections(builder, cfg, knownProviderTypes, "ProjectionStore");
    }

    [RequiresAssemblyFiles("Calls Filin.Hosting.Internal.ReferencedAssemblyProvider.GetRelevantAssemblies()")]
    static Dictionary<(string Kind, string? Name), Type> GetRegisteredProviders(RelevantAssemblies relevantAssemblies)
    {
        var result = new Dictionary<(string, string?), Type>();

        foreach (var asm in relevantAssemblies.Assembles)
        {
            foreach (var attr in asm.GetCustomAttributes<RegisterProviderAttribute>())
            {
                result[(attr.Kind, attr.Name)] = attr.Type;
            }
        }

        return result;
    }

    static void ApplySubsection(IFilinBuilder builder, IConfigurationSection cfg, Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string sectionName)
    {
        if (cfg.GetSection(sectionName) is { } section && section.Exists())
        {
            ConfigureProvider(builder: builder, knownProviderTypes: knownProviderTypes, kind: sectionName, name: null, configurationSection: section);
        }
    }

    static void ApplyNamedSubsections(IFilinBuilder builder, IConfigurationSection cfg, Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string sectionName)
    {
        if (cfg.GetSection(sectionName) is { } section && section.Exists())
        {
            foreach (var child in section.GetChildren())
            {
                ConfigureProvider(builder: builder, knownProviderTypes: knownProviderTypes, kind: sectionName, name: child.Key, configurationSection: child);
            }
        }
    }

    static void ConfigureProvider(IFilinBuilder builder, Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string kind, string? name, IConfigurationSection configurationSection)
    {
        var providerType = configurationSection["ProviderType"];
        var provider = GetRequiredProvider(knownProviderTypes: knownProviderTypes, kind: kind, name: providerType);
        provider.Configure(builder, name, configurationSection);
    }

    static IProviderBuilder<IFilinBuilder> GetRequiredProvider(Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string kind, string? name)
    {
        if (knownProviderTypes.TryGetValue((kind, name), out var type))
        {
            var instance = Activator.CreateInstance(type);

            return instance as IProviderBuilder<IFilinBuilder>
                ?? throw new InvalidOperationException($"{kind} provider, '{name}', of type {type}, does not implement {typeof(IProviderBuilder<IFilinBuilder>)}.");
        }

        throw new InvalidOperationException($"Could not find {kind} provider named '{name}'. This can indicate the provider's package are not referenced by your application.");
    }

    sealed record RelevantAssemblies(ImmutableHashSet<Assembly> Assembles);
}
