using System.Globalization;

namespace Filin.AdoNet;

/// <summary>
/// Formats .NET types appropriately for database consumption in non-parameterized queries.
/// </summary>
sealed class AdoNetFormatProvider : IFormatProvider
{
    readonly AdoNetFormatter _formatter = new();

    /// <inheritdoc/>
    public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? _formatter : null;

    sealed class AdoNetFormatter : ICustomFormatter
    {
        /// <inheritdoc/>
        public string Format(string? format, object? arg, IFormatProvider? formatProvider) =>
            arg switch
            {
                null => "NULL",
                string str => "N'" + str.Replace("'", "''", StringComparison.Ordinal) + "'",
                DateTime dt => "'" + dt.ToString("O") + "'",
                DateTimeOffset dtOffset => "'" + dtOffset.ToString("O") + "'",
                IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
                _ => arg.ToString() ?? arg.GetType().FullName ?? string.Empty,
            };
    }
}
