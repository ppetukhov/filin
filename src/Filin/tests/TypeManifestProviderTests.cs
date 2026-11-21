using Filin.Configuration;

namespace Filin.Tests;

public class TypeManifestProviderTests
{
    [Fact]
    public void PostConfigure_ShouldClearTypeManifestProviders()
    {
        // Arrange
        var provider = new TypeManifestProvider([]);
        var options = new TypeManifestOptions();
        options.TypeManifestProviders.Add(new DummyTypeManifestProvider());

        // Act
        provider.PostConfigure(name: null, options: options);

        // Assert
        Assert.Empty(options.TypeManifestProviders);
    }

    sealed class DummyTypeManifestProvider : ITypeManifestProvider
    {
        public void Configure(TypeManifestOptions options) { }
    }
}
