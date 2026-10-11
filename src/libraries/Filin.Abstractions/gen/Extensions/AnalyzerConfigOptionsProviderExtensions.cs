using Microsoft.CodeAnalysis.Diagnostics;

namespace Filin.Generators.Extensions;

static class AnalyzerConfigOptionsProviderExtensions
{
    extension(AnalyzerConfigOptionsProvider options)
    {
        /// <summary>
        /// Checks whether the input property has a valid <see cref="bool"/> value.
        /// </summary>
        /// <param name="propertyName">The MSBuild property name.</param>
        /// <param name="propertyValue">The resulting property value, if invalid.</param>
        /// <returns>Whether the target property is a valid <see cref="bool"/> value.</returns>
        internal bool TryGetStringValueMSBuildProperty(string propertyName, out string? propertyValue)
            =>
                // MSBuild property that are visible to the compiler are available here with the "build_property." prefix
                options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out propertyValue) && !string.IsNullOrWhiteSpace(propertyValue);

        public bool TryGetBoolValueMSBuildProperty(string propertyName, out bool propertyValue)
        {
            // MSBuild property that are visible to the compiler are available here with the "build_property." prefix
            if (options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value) && !string.IsNullOrWhiteSpace(value))
            {
                if (string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = true;
                    return true;
                }

                if (string.Equals(value, bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = false;
                    return true;
                }
            }

            propertyValue = default;
            return false;
        }
    }
}
