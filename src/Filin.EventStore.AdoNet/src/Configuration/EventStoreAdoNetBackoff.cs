using System.Diagnostics;

namespace Filin.Configuration;

/// <summary>
/// Backoff parameters.
/// </summary>
[DebuggerDisplay("InitalDelay: {InitalDelay}, MaxDelay: {MaxDelay}")]
public sealed class EventStoreAdoNetBackoff
{
    /// <summary>
    /// Initial delay.
    /// </summary>
    /// <remarks>default: 10 ms.</remarks>
    public TimeSpan InitalDelay { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Backoff max delay.
    /// </summary>
    /// <remarks>default: 1 min.</remarks>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Use jitter.
    /// </summary>
    /// <remarks>default: <see langword="true"/>.</remarks>
    public bool UseJitter { get; set; } = true;
}
