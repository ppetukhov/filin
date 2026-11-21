namespace Filin.Tests;

public class SubscribeStudentToCourseDeciderTests
{
    [Fact]
    public void Decide_FallbackWithoutState_ShouldReturnCourseNotFound()
    {
        // Arrange
        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = SubscribeStudentToCourseDecider.Decide(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("COURSE_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public void Decide_StudentNotFound_ShouldReturnStudentNotFoundError()
    {
        // Arrange
        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());
        var state = new StudentCourseSubscriptionState
        {
            StudentId = Guid.Empty, // Студент не найден
            CourseId = command.CourseId
        };

        // Act
        var result = SubscribeStudentToCourseDecider.Decide(command, state);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("STUDENT_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public void Decide_ValidSubscription_ShouldProduceEvent()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var command = new SubscribeStudentToCourse(courseId, studentId);
        var state = new StudentCourseSubscriptionState
        {
            StudentId = studentId,
            CourseId = courseId
        };

        // Act
        var result = SubscribeStudentToCourseDecider.Decide(command, state);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Count);
    }
}
