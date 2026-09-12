using Filin;

using static Edu.DomainEvents;

namespace Edu;

static partial class DomainEvents
{
    internal static partial class V1
    {
        [EventType("CourseCreated")]
        public sealed record CourseCreated([EventTag("course")] Guid CourseId, string Name);

        [EventType("CourseRenamed")]
        public sealed record CourseRenamed([EventTag("course")] Guid CourseId, string NewName, string OldName) { }

        [EventType("CourseCapacityChanged")]
        public sealed record CourseCapacityChanged([EventTag("course")] Guid CourseId, uint Capacity);

        [EventType("StudentRegistered")]
        public sealed record StudentRegistered(string Name)
        {
            [EventTag("student")]
            public Guid StudentId { get; init; }
        }

        [EventType("StudentSubscribed")]
        public sealed record StudentSubscribed(
            [EventTag("course")] Guid CourseId,
            [EventTag("student")] Guid StudentId);

        [EventType("StudentUnsubscribed")]
        public sealed record StudentUnsubscribed(
            [EventTag("course")] Guid CourseId,
            [EventTag("student")] Guid StudentId);
    }
}

public static class EduEventsToDcbEventExtensions
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static DcbEvent ToDcbEvent(this V1.CourseCreated evt, TagCollection tags) => new DcbEvent
    {
        EventType = evt.GetEventType(),
        Data = evt,
        Timestamp = DateTimeOffset.UtcNow,
        Tags = tags is { IsEmpty: false } ? [.. tags, .. evt.GetTags()] : evt.GetTags()
    };

    internal static DcbEvent ToDcbEvent(this V1.CourseCreated evt, IWithQueryTags withQuery) => evt.ToDcbEvent(withQuery, DateTimeOffset.UtcNow);

    internal static DcbEvent ToDcbEvent(this V1.CourseCreated evt, IWithQueryTags withQuery, DateTimeOffset timestamp)
    {
        throw new NotImplementedException();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static DcbEvent ToDcbEvent(this V1.CourseRenamed evt, TagCollection tags) => new DcbEvent
    {
        EventType = evt.GetEventType(),
        Data = evt,
        Timestamp = DateTimeOffset.UtcNow,
        Tags = tags is { IsEmpty: false } ? [.. tags, .. evt.GetTags()] : evt.GetTags()
    };

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static DcbEvent ToDcbEvent(this V1.CourseCapacityChanged evt) => new DcbEvent
    {
        EventType = evt.GetEventType(),
        Data = evt,
        Timestamp = DateTimeOffset.UtcNow,
        Tags = evt.GetTags()
    };

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static DcbEvent ToDcbEvent(this V1.StudentSubscribed evt, TagCollection tags) => new DcbEvent
    {
        EventType = evt.GetEventType(),
        Data = evt,
        Timestamp = DateTimeOffset.UtcNow,
        Tags = tags is { IsEmpty: false } ? [.. tags, .. evt.GetTags()] : evt.GetTags()
    };
}
