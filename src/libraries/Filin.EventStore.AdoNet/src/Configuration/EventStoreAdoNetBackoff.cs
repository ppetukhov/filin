using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Filin.Configuration;

/// <summary>
/// Backoff parameters.
/// </summary>
[DebuggerDisplay("Exponent: {Exponent}, MaxDelay: {MaxDelay}")]
[RequiresUnreferencedCode("Requires dynamic access to code that use reflection for TimeSpan")]
public sealed class EventStoreAdoNetBackoff
{
    /// <summary>
    /// Backoff exponent for delay in seconds.
    /// </summary>
    /// <remarks>default: 1.5, min: 1.0, max: 5</remarks>
    [Range(1.0, 5)]
    public double Exponent { get; set; } = 1.5;

    /// <summary>
    /// Initial delay.
    /// </summary>
    /// <remarks>default: 10 ms, min: 1 ms, max: 1 min</remarks>
    [Range(typeof(TimeSpan), "00:00:00.003", "00:01:00")]
    public TimeSpan InitalDelay { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Backoff max delay.
    /// </summary>
    /// <remarks>default: 1 min, min: 0 sec, max: 30 min</remarks>
    [Range(typeof(TimeSpan), "00:00:00", "00:30:00")]
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Use jitter.
    /// </summary>
    /// <remarks>default: <see langword="true"/>.</remarks>
    public bool UseJitter { get; set; } = true;
}
