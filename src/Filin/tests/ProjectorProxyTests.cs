using Filin.Internal;

namespace Filin.Tests;

public class ProjectorProxyTests
{
    [Fact]
    public void Proxy_Properties_ShouldReturnTargetProjectorMetadata()
    {
        // Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IProjectorProxy<StudentCourseSubscriptionState> proxy =
            new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        // Act & Assert
        Assert.Equal("SubscribeStudentToCourseProjector", proxy.ProjectorId.Value);
    }
}
