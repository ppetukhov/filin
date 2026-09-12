using Filin;

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

