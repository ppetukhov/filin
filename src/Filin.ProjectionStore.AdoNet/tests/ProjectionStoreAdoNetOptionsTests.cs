using System.ComponentModel.DataAnnotations;

using Filin.AdoNet;
using Filin.Configuration;

using Microsoft.Extensions.Configuration;

using Moq;

namespace Filin.ProjectionStore.AdoNet.Tests;

public class ProjectionStoreAdoNetOptionsTests
{
    [Fact]
    public void IsValidInvariant_WhenValidInvariant_ShouldReturnSuccess()
    {
        // Act - Validate standard Npgsql / PostgreSQL invariant
        var result = ProjectionStoreAdoNetOptions.IsValidInvariant(AdoNetInvariants.InvariantNamePostgreSql);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValidInvariant_WhenInvalidInvariant_ShouldReturnValidationError()
    {
        // Act
        var result = ProjectionStoreAdoNetOptions.IsValidInvariant("InvalidDatabaseProvider");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("is not supported", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenNoConnectionStringOrConnectionNameOrDataSource_ShouldReturnError()
    {
        // Arrange
        var options = new ProjectionStoreAdoNetOptions
        {
            Invariant = AdoNetInvariants.InvariantNamePostgreSql,
            StorageSerializer = Mock.Of<Filin.Storage.IStorageSerializer>(),
            DataSource = null!
        };

        var context = new ValidationContext(options);

        // Act
        var results = new List<ValidationResult>(options.Validate(context));

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Required 'ConnectionString' or 'ConnectionName' or 'DataSource'", StringComparison.Ordinal));
    }

    [Fact]
    public void ConnectionNameValidator_WhenConnectionNameNotFound_ShouldReturnFail()
    {
        // Arrange - Empty configuration without requested connection string
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var validator = new ProjectionStoreAdoNetOptionsConnectionNameValidator(config);
        var options = new ProjectionStoreAdoNetOptions
        {
            ConnectionName = "NonExistentDb",
            StorageSerializer = Mock.Of<Filin.Storage.IStorageSerializer>(),
            DataSource = null!
        };

        // Act
        var result = validator.Validate(nameof(ProjectionStoreAdoNetOptions), options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains("not found", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionNameValidator_WhenConnectionNameExists_ShouldReturnSuccess()
    {
        // Arrange - Configuration containing valid connection string
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LocalDev"] = "Host=localhost;Database=test"
            })
            .Build();

        var validator = new ProjectionStoreAdoNetOptionsConnectionNameValidator(config);
        var options = new ProjectionStoreAdoNetOptions
        {
            ConnectionName = "LocalDev",
            StorageSerializer = Mock.Of<Filin.Storage.IStorageSerializer>(),
            DataSource = null!
        };

        // Act
        var result = validator.Validate(nameof(ProjectionStoreAdoNetOptions), options);

        // Assert
        Assert.True(result.Succeeded);
    }
}
