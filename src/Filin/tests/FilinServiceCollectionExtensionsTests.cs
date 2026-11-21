using Filin.DependencyInjection;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.DependencyInjection;

namespace Filin.Tests;

public class FilinServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFilin_ShouldRegisterRequiredCoreServices()
    {
        // Arrange - Setup service collection and mock storage dependencies with Moq
        var services = new ServiceCollection();

        // Register mock storage services required by HandlersFactory
        services.AddSingleton<IEventStore>(new StubEventStore());
        services.AddSingleton<IAppendEventsBlock>(new StubAppendEventsBlock());

        // Act - Register Filin framework dependencies
        services.AddFilin();
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify Mediator and Serializer resolve successfully from DI container
        Assert.NotNull(serviceProvider.GetService<IMediator>());
        Assert.NotNull(serviceProvider.GetService<IStorageSerializer>());
    }
}
