using System.Diagnostics;

namespace Filin.Internal;

/// <summary>
/// Serves as a debugger type proxy to display a clean, flat array view of tags during debugging sessions.
/// </summary>
/// <param name="collection">The target collection to display.</param>
sealed class TagCollectionDebugView(TagCollection collection)
{
    /// <summary>
    /// Gets the flat array of tags to be hidden as a root and expanded directly by the IDE debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Tag[] Items => collection.AsSpan().ToArray();
}
