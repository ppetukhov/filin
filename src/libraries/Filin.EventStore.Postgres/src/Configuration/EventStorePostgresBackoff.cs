using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Filin.Configuration;

/// <summary>
/// Backoff parameters.
/// </summary>
[DebuggerDisplay("Exponent: {Exponent}, MaxDelay: {MaxDelay}")]
public sealed class EventStorePostgresBackoff
{
    /// <summary>
    /// Backoff exponent for delay in seconds.
    /// </summary>
    /// <remarks>default: 1.5, min: 1.0, max: 5</remarks>
    [Range(1.0, 5)]
    public double Exponent { get; set; } = 1.5;

    /// <summary>
    /// Backoff max delay.
    /// </summary>
    /// <remarks>default: 1 min, min: 0 sec, max: 30 min</remarks>
    [Range(typeof(TimeSpan), "00:00:00", "00:30:00")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
}
