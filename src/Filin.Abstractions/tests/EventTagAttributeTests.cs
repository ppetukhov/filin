namespace Filin.Abstractions.Tests;

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

        Assert.Equal(2, tags.Count);
        Assert.True(tags.Contains(new Tag($"course:{courseId}")));
        Assert.True(tags.Contains(new Tag($"course-name:{name}")));
    }

    [Fact]
    public void EventWithOneTagGetTagReturnTag()
    {
        var studentId = Guid.NewGuid();

        var evt = new EventWithOneTag { StudentId = studentId, Name = "Smith" };

        var tag = evt.GetTag();

        Assert.Equal(new Tag($"student:{studentId}"), tag);
    }
}
