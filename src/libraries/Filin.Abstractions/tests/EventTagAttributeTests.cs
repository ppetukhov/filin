namespace Filin.Abstractions.EventTagTests;

sealed record CourseCreated([EventTag("course")] Guid CourseId)
{
    [EventTag("course-name")]
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int Capacity { get; init; }
}

sealed record EventWithOneTag
{
    [EventTag("student")]
    public Guid StudentId { get; init; }
    public required string Name { get; init; }
}

public sealed class EventTagAttributeTests
{
    [Fact]
    public void CourseCreatedGetTagsReturnTwoTags()
    {
        var courseId = Guid.NewGuid();
        var name = "Test course";

        var evt = new CourseCreated(courseId) { Name = name, Description = "Cool course", Capacity = 20 };

        var tags = evt.GetTags();

        tags.Count.Should().Be(2);
        tags.Contains(new Tag($"course:{courseId}")).Should().BeTrue();
        tags.Contains(new Tag($"course-name:{name}")).Should().BeTrue();
    }

    [Fact]
    public void EventWithOneTagGetTagReturnTag()
    {
        var studentId = Guid.NewGuid();

        var evt = new EventWithOneTag { StudentId = studentId, Name = "Smith" };

        var tag = evt.GetTag();

        tag.Should().Be(new Tag($"student:{studentId}"));
    }
}
