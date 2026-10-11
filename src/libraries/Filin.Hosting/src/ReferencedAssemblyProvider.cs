using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.Extensions.DependencyModel;

namespace Filin.Hosting;

static class ReferencedAssemblyProvider
{
    [RequiresAssemblyFiles("Calls Filin.Hosting.ReferencedAssemblyProvider.AddFromDependencyContext(HashSet<Assembly>, Assembly)")]
    internal static IEnumerable<Assembly> GetRelevantAssemblies()
    {
        var parts = new HashSet<Assembly>();

        AddFromDependencyContext(parts);

        AddFromAssemblyLoadContext(parts);

        foreach (var loadedAsm in AppDomain.CurrentDomain.GetAssemblies())
        {
            AddAssembly(parts, loadedAsm);
        }

        return parts;
    }

    internal static void AddAssembly(HashSet<Assembly> parts, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(assembly);

        if (!assembly.IsDefined(typeof(ApplicationPartAttribute)))
        {
            return;
        }

        if (!parts.Add(assembly))
        {
            return;
        }

        AddAssembly(parts, assembly);

        // Add all referenced application parts.
        foreach (var referencedAsm in GetApplicationPartAssemblies(assembly))
        {
            AddAssembly(parts, referencedAsm);
        }
    }

    public static void AddFromAssemblyLoadContext(HashSet<Assembly> parts, Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(parts);

        assembly ??= typeof(ReferencedAssemblyProvider).Assembly;
        var assemblies = new HashSet<Assembly>();
        var context = AssemblyLoadContext.GetLoadContext(assembly) ?? throw new InvalidOperationException($"Failed get LoadContext for assembly {assembly.FullName}");

        foreach (var asm in context.Assemblies)
        {
            // Skip assemblies which have not had code generation executed against them and already-seen assemblies.
            if (!asm.IsDefined(typeof(ApplicationPartAttribute)) || !assemblies.Add(asm))
            {
                continue;
            }

            AddAssembly(parts, asm);
        }
    }

    [RequiresAssemblyFiles("Calls Microsoft.Extensions.DependencyModel.DependencyContext.Default")]
    internal static void AddFromDependencyContext(HashSet<Assembly> parts, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly();
        var dependencyContext = assembly is null || assembly.IsDynamic ? DependencyContext.Default : DependencyContext.Load(assembly);

        var assemblies = new HashSet<Assembly>();

        if (assembly != null && assembly.IsDefined(typeof(ApplicationPartAttribute)))
        {
            AddAssembly(parts, assembly);
            _ = assemblies.Add(assembly);
        }

        if (dependencyContext == null)
        {
            return;
        }

        var assemblyContext = assembly is not null
            ? AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default
            : AssemblyLoadContext.Default;

        foreach (var lib in dependencyContext.RuntimeLibraries)
        {
            if (lib.Name.StartsWith("Microsoft", StringComparison.Ordinal) || lib.Name.StartsWith("System", StringComparison.Ordinal))
            {
                continue;
            }

#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                var name = lib.GetRuntimeAssemblyNames(dependencyContext, System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier).FirstOrDefault();
                if (name is null)
                {
                    continue;
                }

                var asm = assemblyContext.LoadFromAssemblyName(name);

                if (asm.IsDefined(typeof(ApplicationPartAttribute)) && assemblies.Add(asm))
                {
                    AddAssembly(parts, asm);
                }
            }
            catch
            {
                // Ignore any exceptions thrown during non-explicit assembly loading.
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }

    static IEnumerable<Assembly> GetApplicationPartAssemblies(Assembly assembly)
    {
        if (!assembly.IsDefined(typeof(ApplicationPartAttribute)))
        {
            return [];
        }

        var assemblyContext = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;

        return ExpandApplicationParts(
            new[] { assembly }.Concat(assembly.GetCustomAttributes<ApplicationPartAttribute>()
                .Select(name =>
                    assemblyContext.LoadFromAssemblyName(new AssemblyName(name.AssemblyName))

                )));

        static IEnumerable<Assembly> ExpandApplicationParts(IEnumerable<Assembly> assemblies)
        {
            var relatedAssemblies = new HashSet<Assembly>();

            foreach (var assembly in assemblies)
            {
                if (relatedAssemblies.Add(assembly))
                {
                    ExpandAssembly(relatedAssemblies, assembly);
                }
            }

            return relatedAssemblies.OrderBy(assembly => assembly.FullName, StringComparer.Ordinal);

            static void ExpandAssembly(HashSet<Assembly> assemblies, Assembly assembly)
            {
                var attributes = assembly.GetCustomAttributes<ApplicationPartAttribute>().ToArray();
                if (attributes.Length == 0)
                {
                    return;
                }

                var assemblyContext = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;

                foreach (var attribute in attributes)
                {
                    var assemblyName = new AssemblyName(attribute.AssemblyName);
                    var referenced = assemblyContext.LoadFromAssemblyName(assemblyName);

                    if (assemblies.Add(referenced))
                    {
                        ExpandAssembly(assemblies, referenced);
                    }
                }
            }
        }
    }
}
