using System.Diagnostics;

namespace Filin.Internal;

/// <summary>
/// Serves as a debugger type proxy to display a clean, flat array view of event types during debugging sessions.
/// </summary>
/// <param name="collection">The target collection to display.</param>
sealed class EventTypesDebugView(EventTypes collection)
{
    /// <summary>
    /// Gets the flat array of event types to be expanded directly by the IDE debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public EventType[] Items => collection.AsSpan().ToArray();
}
