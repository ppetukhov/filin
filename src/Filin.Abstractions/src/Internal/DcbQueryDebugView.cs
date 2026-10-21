using System.Diagnostics;

namespace Filin.Internal;

/// <summary>
/// Serves as a debugger type proxy to display a clean, flat array view of query items during debugging sessions.
/// </summary>
/// <param name="query">The target query specification to display.</param>
sealed class DcbQueryDebugView(DcbQuery query)
{
    /// <summary>
    /// Gets the flat array of query items to be expanded directly by the IDE debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public DcbQueryItem[] Items => query.AsSpan().ToArray();

    /// <summary>
    /// Static utility method called by the debugger to render the composite query preview string.
    /// </summary>
    public static string GetDisplayString(DcbQuery query)
    {
        return query.Count switch
        {
            0 => "Any event",
            1 => DcbQueryItemDebugView.GetDisplayString(query[0]),
            int count => $"Count = {count}"
        };
    }
}
