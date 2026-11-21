using System.Data.Common;

using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Options;

using Moq;

namespace Filin.ProjectionStore.AdoNet.Tests;

public class ProjectionStoreAdoNetOptionsProviderTests
{
    [Fact]
    public void PostConfigure_ShouldCopyProjectionTypesFromTypeManifestOptions()
    {
        // Arrange - Setup TypeManifestOptions with registered projection type
        var projectorId = new ProjectorId("ActiveCoursesProjector");
        var manifestOptions = new TypeManifestOptions();
        manifestOptions.ProjectionTypes[projectorId] = typeof(ActiveCourses);

        var typeManifestOptions = Options.Create(manifestOptions);
        var serviceProviderMock = new Mock<IServiceProvider>();

        var provider = new ProjectionStoreAdoNetOptionsProvider(serviceProviderMock.Object, typeManifestOptions);

        var options = new ProjectionStoreAdoNetOptions
        {
            StorageSerializer = Mock.Of<IStorageSerializer>(),
            DataSource = Mock.Of<DbDataSource>()
        };

        // Act - PostConfigure options
        provider.PostConfigure(nameof(ProjectionStoreAdoNetOptions), options);

        // Assert - Verify projection type was copied to options
        Assert.True(options.ProjectionTypes.ContainsKey(projectorId));
        Assert.Equal(typeof(ActiveCourses), options.ProjectionTypes[projectorId]);
    }

    [Fact]
    public void PostConfigure_ShouldFallbackToDefaultStorageSerializer_WhenSerializerKeyIsNull()
    {
        // Arrange - Setup IServiceProvider to resolve default IStorageSerializer
        var manifestOptions = Options.Create(new TypeManifestOptions());
        var defaultSerializer = Mock.Of<IStorageSerializer>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IStorageSerializer)))
            .Returns(defaultSerializer);

        var provider = new ProjectionStoreAdoNetOptionsProvider(serviceProviderMock.Object, manifestOptions);

        var options = new ProjectionStoreAdoNetOptions
        {
            SerializerKey = null,
            StorageSerializer = null!,
            DataSource = Mock.Of<DbDataSource>()
        };

        // Act - PostConfigure options
        provider.PostConfigure(nameof(ProjectionStoreAdoNetOptions), options);

        // Assert - Verify default serializer was assigned
        Assert.Same(defaultSerializer, options.StorageSerializer);
    }

    sealed class ActiveCourses { }
}
