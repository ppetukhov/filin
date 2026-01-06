using System.Globalization;

using Microsoft.CodeAnalysis.CSharp;

namespace SourceGenerators;

static class CSharpSyntaxUtilities
{
    // Standard format for double and single on non-inbox frameworks to ensure value is round-trippable.
    public const string DoubleFormatString = "G17";
    public const string SingleFormatString = "G9";

    // Format a literal in C# format -- works around https://github.com/dotnet/roslyn/issues/58705
    public static string FormatLiteral(object? value, TypeRef type)
    {
        return value == null
            ? $"default({type.FullyQualifiedName})"
            : value switch
            {
                string @string => SymbolDisplay.FormatLiteral(@string, quote: true),
                char @char => SymbolDisplay.FormatLiteral(@char, quote: true),
                double.NegativeInfinity => "double.NegativeInfinity",
                double.PositiveInfinity => "double.PositiveInfinity",
                double.NaN => "double.NaN",
                double @double => $"{@double.ToString(DoubleFormatString, CultureInfo.InvariantCulture)}D",
                float.NegativeInfinity => "float.NegativeInfinity",
                float.PositiveInfinity => "float.PositiveInfinity",
                float.NaN => "float.NaN",
                float @float => $"{@float.ToString(SingleFormatString, CultureInfo.InvariantCulture)}F",
                decimal @decimal => $"{@decimal.ToString(CultureInfo.InvariantCulture)}M",// we do not need to specify a format string for decimal as it's default is round-trippable on all frameworks.
                bool @bool => @bool ? "true" : "false",
                _ => FormatNumber(),// Assume this is a number.
            };

        string FormatNumber() => $"({type.FullyQualifiedName})({Convert.ToString(value, CultureInfo.InvariantCulture)})";
    }
}
