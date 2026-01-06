using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

#pragma warning disable CA1812

/// <inheritdoc/>
sealed class OptionsFormatterResolver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IOptionsMonitor<T> optionsMonitor, IServiceProvider serviceProvider) : IOptionsFormatterResolver<T> where T : class
{
    /// <inheritdoc/>
    public IOptionsFormatter<T> Resolve(string? name) => new OptionsFormatter<T>(name, optionsMonitor.Get(name), serviceProvider);
}

#pragma warning restore CA1812
