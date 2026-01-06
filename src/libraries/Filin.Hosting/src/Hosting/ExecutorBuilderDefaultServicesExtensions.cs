using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Filin.Compliance.Classification;
using Filin.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Filin.Hosting;

public static class ExecutorBuilderDefaultServicesExtensions
{
    static readonly ServiceDescriptor ServiceDescriptor = new(typeof(ServicesAdded), new ServicesAdded());

    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.ApplyConfiguration(IExecutorBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.SerializationServiceCollectionExtentions.AddStorageSerialization()")]
    [RequiresDynamicCode("Calls Filin.Hosting.SerializationServiceCollectionExtentions.AddStorageSerialization()")]
    public static IExecutorBuilder AddDefaultServices(this IExecutorBuilder builder)
    {
        var services = builder.Services;

        if (services.Contains(ServiceDescriptor))
        {
            return builder;
        }

        services.Add(ServiceDescriptor);

        // Common services
        services.AddOptions();
        services.TryAddSingleton(TimeProvider.System);

        services.AddLogging(builder => builder.EnableRedaction());
        services.AddRedaction(builder => builder.SetRedactor<ConnectionStringRedactor>(AppConfigClassifications.ConnectionString));

        services.TryAddSingleton(typeof(IOptionsFormatter<>), typeof(OptionsFormatter<>));
        services.TryAddSingleton(typeof(IOptionsFormatterResolver<>), typeof(OptionsFormatterResolver<>));
        services.TryAddSingleton<OptionsLogger>();

        services.AddStorageSerialization();

        var relevantAssembles = new RelevantAssemblies([.. Internal.ReferencedAssemblyProvider.GetRelevantAssemblies()]);

        foreach (var assembly in relevantAssembles.Assembles)
        {
            _ = builder.AddAssembly(assembly);
        }

        ApplyConfiguration(builder, relevantAssembles);

        return builder;
    }

    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.GetRegisteredProviders()")]
    static void ApplyConfiguration(IExecutorBuilder builder, RelevantAssemblies relevantAssemblies)
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
            foreach (var attr in asm.GetCustomAttributes<ProviderBuilderAttribute>())
            {
                if (string.Equals(attr.Target, "Executor", StringComparison.OrdinalIgnoreCase))
                {
                    result[(attr.Kind, attr.Name)] = attr.Type;
                }
            }
        }

        return result;
    }

    static void ApplySubsection(IExecutorBuilder builder, IConfigurationSection cfg, Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string sectionName)
    {
        if (cfg.GetSection(sectionName) is { } section && section.Exists())
        {
            ConfigureProvider(builder: builder, knownProviderTypes: knownProviderTypes, kind: sectionName, name: null, configurationSection: section);
        }
    }

    static void ApplyNamedSubsections(IExecutorBuilder builder, IConfigurationSection cfg, Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string sectionName)
    {
        if (cfg.GetSection(sectionName) is { } section && section.Exists())
        {
            foreach (var child in section.GetChildren())
            {
                ConfigureProvider(builder: builder, knownProviderTypes: knownProviderTypes, kind: sectionName, name: child.Key, configurationSection: child);
            }
        }
    }

    static void ConfigureProvider(IExecutorBuilder builder, Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string kind, string? name, IConfigurationSection configurationSection)
    {
        var providerType = configurationSection["ProviderType"];
        var provider = GetRequiredProvider(knownProviderTypes: knownProviderTypes, kind: kind, name: providerType);
        provider.Configure(builder, name, configurationSection);
    }

    static IProviderBuilder<IExecutorBuilder> GetRequiredProvider(Dictionary<(string Kind, string? Name), Type> knownProviderTypes, string kind, string? name)
    {
        if (knownProviderTypes.TryGetValue((kind, name), out var type))
        {
            var instance = Activator.CreateInstance(type);

            return instance as IProviderBuilder<IExecutorBuilder>
                ?? throw new InvalidOperationException($"{kind} provider, '{name}', of type {type}, does not implement {typeof(IProviderBuilder<IExecutorBuilder>)}.");
        }

        throw new InvalidOperationException($"Could not find {kind} provider named '{name}'. This can indicate the provider's package are not referenced by your application.");
    }

    sealed class ServicesAdded { }

    sealed record RelevantAssemblies(ImmutableHashSet<Assembly> Assembles);
}
