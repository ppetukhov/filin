using Filin.Configuration;
using Filin.DependencyInjection;
using Filin.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Filin.ProjectionStore.AdoNet.Tests;

public class FilinProjectionStoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddProjectionStoreAdoNet_ShouldRegisterServicesAndSingletonStore()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ProjectionStoreAdoNetOptions.SectionName}:Invariant"] = "Npgsql",
                [$"{ProjectionStoreAdoNetOptions.SectionName}:ConnectionString"] = "Host=localhost;Database=test"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Mock.Of<IStorageSerializer>());

        // Act - Register AdoNet ProjectionStore
        services.AddProjectionStoreAdoNet();

        var provider = services.BuildServiceProvider();

        // Assert - Verify IProjectionStore is registered as Singleton
        var storeDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IProjectionStore));
        Assert.Equal(ServiceLifetime.Singleton, storeDescriptor.Lifetime);
    }
}
