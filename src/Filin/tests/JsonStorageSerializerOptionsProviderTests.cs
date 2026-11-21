using System.Text.Json.Serialization.Metadata;

using Filin.Configuration;

namespace Filin.Tests;

public class JsonStorageSerializerOptionsProviderTests
{
    [Fact]
    public void PostConfigure_ShouldMakeSerializerOptionsReadOnly()
    {
        // Arrange
        var provider = new JsonStorageSerializerOptionsProvider();
        var options = new JsonStorageSerializerOptions();
        options.SerializerOptions.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();

        // Act
        provider.PostConfigure(name: null, options: options);

        // Assert
        Assert.True(options.SerializerOptions.IsReadOnly);
    }
}
