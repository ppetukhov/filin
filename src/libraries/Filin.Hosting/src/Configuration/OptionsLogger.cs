using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Filin.Configuration;

/// <summary>
/// Options logger.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OptionsLogger"/> class.
/// </remarks>
/// <param name="loggerFactory"><inheritdoc cref="ILoggerFactory" path="/summary"/></param>
/// <param name="services"><inheritdoc cref="IServiceProvider" path="/summary"/></param>
public partial class OptionsLogger(ILoggerFactory loggerFactory, IServiceProvider services)
{
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.Configuration");

    /// <summary>
    /// Log all options with registered formatters.
    /// </summary>
    public void LogOptions() => LogOptions(services.GetServices<IOptionsFormatter>());

    /// <summary>
    /// Log options using a set of formatters.
    /// </summary>
    /// <param name="formatters">The collection of options formatters.</param>
    public virtual void LogOptions(IEnumerable<IOptionsFormatter> formatters)
    {
        ArgumentNullException.ThrowIfNull(formatters);

        var writer = new SbWrapper(new StringBuilder(capacity: 256));

        foreach (var formatter in formatters.OrderBy(f => f.Name))
        {
            LogOptionInternal(ref writer, formatter);
        }
    }

    /// <summary>
    /// Log an options using a formatter.
    /// </summary>
    /// <param name="formatter">The options formatter.</param>
    public virtual void LogOption(IOptionsFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var writer = new SbWrapper(new StringBuilder(capacity: 256));

        LogOptionInternal(ref writer, formatter);
    }

    void LogOptionInternal(ref SbWrapper sb, IOptionsFormatter formatter)
    {
        try
        {
            formatter.Format(sb.Write);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var opt = sb.ToString();
                LogInformationOptions(_logger, formatter.Name, opt);
            }

            sb.Reset();
        }
        catch (Exception ex)
        {
            LogErrorOptions(_logger, ex, formatter.Name);
            throw;
        }
    }

    struct SbWrapper(StringBuilder sb)
    {
        internal readonly void Write(ReadOnlySpan<char> value) => sb.Append(value);

        /// <inheritdoc/>
        public override readonly string ToString() => sb.ToString();

        internal readonly void Reset() => sb.Length = 0;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        EventId = 100,
        EventName = "Configuration",
        Message = "Configuration {ConfigurationName}: {Options}",
        SkipEnabledCheck = true
    )]
    private static partial void LogInformationOptions(ILogger logger, string ConfigurationName, string Options);

    [LoggerMessage(
        Level = LogLevel.Error,
        EventId = 101,
        EventName = "ConfigurationFormatError",
        Message = "An error occurred while logging '{ConfigurationName}' options."
    )]
    private static partial void LogErrorOptions(ILogger logger, Exception exception, string ConfigurationName);
}
