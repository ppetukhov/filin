using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// Default implementation of <see cref="IOptionsFormatter{T}"/>.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
sealed class OptionsFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IOptionsFormatter<T> where T : class
{
    readonly T _options;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsFormatter{T}"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="serviceProvider"><inheritdoc cref="IServiceProvider" path="/summary"/></param>
    public OptionsFormatter(IOptions<T> options, IServiceProvider serviceProvider)
    {
        _options = options.Value;
        Name = typeof(T).FullName ?? string.Empty;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsFormatter{T}"/> class.
    /// </summary>
    /// <param name="name">The options name.</param>
    /// <param name="options">The options.</param>
    /// <param name="serviceProvider"><inheritdoc cref="IServiceProvider" path="/summary"/></param>
    internal OptionsFormatter(string? name, T options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        Name = string.IsNullOrEmpty(name) ? typeof(T).FullName ?? string.Empty : $"{typeof(T).FullName}-{name}";
    }

    /// <summary>
    /// Gets the options name.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public void Format(Action<ReadOnlySpan<char>> writer)
    {
        var redactorProvider = _serviceProvider.GetRequiredService<IRedactorProvider>();

        var isFirst = true;

        foreach (var prop in typeof(T).GetProperties())
        {
            if (!IsFormattableProperty(prop))
            {
                continue;
            }

            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                writer(" ");
            }

            FormatProperty(writer, _options, prop, redactorProvider);

            writer(";");
        }
    }

    static bool IsFormattableProperty(PropertyInfo prop) => prop is not null && IsFormattableType(prop.PropertyType) && prop.GetCustomAttribute<ObsoleteAttribute>() is null && IsAccessibleMethod(prop.GetSetMethod()) && IsAccessibleMethod(prop.GetGetMethod());

    static bool IsAccessibleMethod(MethodInfo? accessor) => accessor is not null && accessor.IsPublic && accessor.GetCustomAttribute<ObsoleteAttribute>() is null;

    static bool IsFormattableType(Type type) => type is not null && !typeof(Delegate).IsAssignableFrom(type);

    static void FormatProperty(Action<ReadOnlySpan<char>> writer, object options, PropertyInfo property, IRedactorProvider redactorProvider)
    {
        var name = property.Name;
        var value = property.GetValue(options);
        var classifiedAttributes = property.GetCustomAttributes<DataClassificationAttribute>(inherit: true);

        // If redact specified, let the attribute implementation do the work
        if (classifiedAttributes.Any())
        {
            var redactor = redactorProvider.GetRedactor(new DataClassificationSet(classifiedAttributes.Select(x => x.Classification)));

            Format(writer, name: name, subKey: null, value: value, redactor: redactor);
        }
        else
        {
            if (value is IDictionary dict)
            {
                // If it is a dictionary -> one line per item
                var enumerator = dict.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Entry;

                    Format(writer, name: name, subKey: kvp.Key, value: kvp.Value);
                }
            }
            else if (value is ICollection coll)
            {
                // If it is a simple collection -> one line per item
                if (coll.Count > 0)
                {
                    var index = 0;
                    foreach (var item in coll)
                    {
                        Format(writer, name: name, subKey: index, value: item);
                        index++;
                    }
                }
            }
            else
            {
                Format(writer, name: name, subKey: null, value: value);
            }
        }
    }

    static void Format(Action<ReadOnlySpan<char>> writer, string name, object? subKey, object? value, Redactor? redactor = null)
    {
        writer(name.AsSpan());

        if (subKey is not null)
        {
            var v = '.';
            writer(new ReadOnlySpan<char>(ref v));

            if (subKey is string subKeyStr)
            {
                writer(subKeyStr.AsSpan());
            }
            else if (subKey is int subKeyIndex)
            {
                Span<char> buffer = stackalloc char[128];

                if (subKeyIndex.TryFormat(buffer, out var charsWritten, default, null))
                {
                    writer(buffer[..charsWritten]);
                }
            }
            else
            {
                writer(subKey.ToString());
            }
        }

        writer(": ");

        if (value is null)
        {
            writer("(null)");
        }
        else if (redactor is not null)
        {
            writer(redactor.Redact(value));
        }
        else
        {
            writer(value.ToString());
        }
    }
}
