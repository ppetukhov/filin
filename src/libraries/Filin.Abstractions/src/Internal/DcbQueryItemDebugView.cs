using System.Diagnostics;

namespace Filin.Internal;

/// <summary>
/// Serves as a debugger type proxy to display a clean, structured view of a query item constraints during debugging sessions.
/// </summary>
/// <param name="queryItem">The target query item to display.</param>
sealed class DcbQueryItemDebugView(DcbQueryItem queryItem)
{
    /// <summary>
    /// Gets the flat array view of interested event types bound to this specific query item.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public EventType[] EventTypes => queryItem.EventTypes.AsSpan().ToArray();

    /// <summary>
    /// Gets the flat array view of domain tags bound to this specific query item.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Tag[] Tags => queryItem.Tags.AsSpan().ToArray();

    /// <summary>
    /// Static utility method called by the debugger to render the precise string representation.
    /// Example output: "EventTypes = [CourseCreated, CourseRenamed], Tags = [course:122]"
    /// </summary>
    public static string GetDisplayString(DcbQueryItem queryItem)
    {
        if (queryItem.IsEmpty)
        {
            return "Any event";
        }

        string typesPart = queryItem.EventTypes.ToString();

        string tagsPart = queryItem.Tags.ToString();

        return (typesPart, tagsPart) switch
        {
            ("", "") => "Any event",
            (string evts, "") => $"Any event from types: [{evts}]",
            ("", string tgs) => $"Any event with all tags: [{tgs}]",
            (string evts, string tgs) => $"Any event from types: [{evts}] with all tags: [{tgs}]"
        };
    }
}
