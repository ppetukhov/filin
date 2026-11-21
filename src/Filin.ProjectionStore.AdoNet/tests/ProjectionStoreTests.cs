using System.Data.Common;

using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Filin.ProjectionStore.AdoNet.Tests;

public class ProjectionStoreTests
{
    [Fact]
    public async Task SaveAsync_SingleNullProjection_ShouldThrowArgumentNullException()
    {
        // Arrange - Instantiate ProjectionStore with options
        var options = Options.Create(new ProjectionStoreAdoNetOptions
        {
            StorageSerializer = Mock.Of<IStorageSerializer>(),
            DataSource = Mock.Of<DbDataSource>()
        });

        var store = new ProjectionStore(options, NullLoggerFactory.Instance);

        // Act & Assert - Expect ArgumentNullException when single projection is null
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            store.SaveAsync((Projection)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_BatchNullProjections_ShouldThrowArgumentNullException()
    {
        // Arrange - Instantiate ProjectionStore
        var options = Options.Create(new ProjectionStoreAdoNetOptions
        {
            StorageSerializer = Mock.Of<IStorageSerializer>(),
            DataSource = Mock.Of<DbDataSource>()
        });

        var store = new ProjectionStore(options, NullLoggerFactory.Instance);

        // Act & Assert - Expect ArgumentNullException when projection collection is null
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            store.SaveAsync((IEnumerable<Projection>)null!, CancellationToken.None));
    }
}
