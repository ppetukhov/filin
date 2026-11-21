using Filin.Internal;

namespace Filin.Tests;

public class DeciderProxyTests
{
    [Fact]
    public void Proxy_Decide_WithoutState_ShouldInvokeStaticDeciderMethod()
    {
        // Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IDeciderProxy<SubscribeStudentToCourse, StudentCourseSubscriptionState> proxy =
            new DeciderProxy<SubscribeStudentToCourseDecider, SubscribeStudentToCourse, StudentCourseSubscriptionState>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = proxy.Decide(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("COURSE_NOT_FOUND", result.Error.Code);
    }
}
