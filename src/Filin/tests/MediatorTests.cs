using Filin.DependencyInjection;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Filin.Tests;

public class MediatorTests
{
    [Fact]
    public async Task SendAsync_ValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange - Setup DI container with Stubs and Filin core services
        using var provider = CreateServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert - Command execution should complete cleanly without throwing
        await mediator.ExecuteAsync<SubscribeStudentToCourse, StudentCourseSubscriptionState>(command, CancellationToken.None);
    }

    [Fact]
    public async Task QueryAsync_ValidQuery_ShouldReturnExpectedResult()
    {
        // Arrange - Setup container and get mediator
        using var provider = CreateServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetActiveCourses();

        // Act - Execute query through mediator
        var result = await mediator.QueryAsync<GetActiveCourses, ActiveCourses>(query, CancellationToken.None);

        // Assert - Verify that query executes and returns a result (or null if uninitialized)
        Assert.Null(result);
    }

    [Fact]
    public async Task SendAsync_NullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var provider = CreateServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mediator.ExecuteAsync<SubscribeStudentToCourse, StudentCourseSubscriptionState>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task QueryAsync_NullQuery_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var provider = CreateServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mediator.QueryAsync<GetActiveCourses, ActiveCourses>(null!, CancellationToken.None));
    }

    // Helper method to setup ServiceProvider with clean Stubs and Filin setup
    static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Register stubs for EventStore and AppendEventsBlock
        services.AddSingleton<IEventStore>(new StubEventStore());
        services.AddSingleton<IAppendEventsBlock>(new StubAppendEventsBlock());
        services.AddSingleton(NullLoggerFactory.Instance);

        // Register Filin framework core
        services.AddFilin();
        services.AddTypeManifestAssembly(typeof(GetActiveCourses).Assembly);

        return services.BuildServiceProvider();
    }
}
