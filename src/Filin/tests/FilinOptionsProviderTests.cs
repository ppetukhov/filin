using Filin.Configuration;
using Filin.Internal;

using Microsoft.Extensions.Options;

namespace Filin.Tests;

public class FilinOptionsProviderTests
{
    [Fact]
    public void Configure_ShouldRegisterProjectorAndDeciderProxyTypes()
    {
        // Arrange
        var manifestOptions = new TypeManifestOptions();
        manifestOptions.ProjectorTypes[typeof(StudentCourseSubscriptionState)] = typeof(SubscribeStudentToCourseProjector);
        manifestOptions.DeciderTypes[(typeof(SubscribeStudentToCourse), typeof(StudentCourseSubscriptionState))] = typeof(SubscribeStudentToCourseDecider);

        var optionsMock = Options.Create(manifestOptions);
        var provider = new FilinOptionsProvider(optionsMock);
        var filinOptions = new FilinOptions();

        // Act
        provider.Configure(filinOptions);

        // Assert
        Assert.True(filinOptions.ProjectorProxyTypes.ContainsKey(typeof(StudentCourseSubscriptionState)));
        Assert.Equal(
            typeof(ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>),
            filinOptions.ProjectorProxyTypes[typeof(StudentCourseSubscriptionState)]
        );

        var deciderKey = (typeof(SubscribeStudentToCourse), typeof(StudentCourseSubscriptionState));
        Assert.True(filinOptions.DeciderProxyTypes.ContainsKey(deciderKey));
        Assert.Equal(
            typeof(DeciderProxy<SubscribeStudentToCourseDecider, SubscribeStudentToCourse, StudentCourseSubscriptionState>),
            filinOptions.DeciderProxyTypes[deciderKey]
        );
    }
}
