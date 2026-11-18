namespace Release.Automation.Tests;

/// <summary>
/// Contains unit tests for <see cref="VersionsPropsDocument"/> verifying XML parsing, version manipulation, and file persistence.
/// </summary>
public class VersionsPropsDocumentTests
{
    #region XML Tag Name Formatting Tests

    [Theory]
    [InlineData("CoreLib", "CoreLibVersion")]
    [InlineData("My.Super.Project", "MySuperProjectVersion")]
    [InlineData("Company.Product.SubModule.Service", "CompanyProductSubModuleServiceVersion")]
    public void GetVersionXmlTagName_ShouldFormatProjectNameToXmlTag(string projectName, string expectedTagName)
    {
        // Act
        var tagName = VersionsPropsDocument.GetVersionXmlTagName(projectName);

        // Assert
        Assert.Equal(expectedTagName, tagName);
    }

    #endregion

    #region Parse & LoadOrCreate Tests

    [Fact]
    public void Parse_ShouldSuccessfullyDeserializeValidXml()
    {
        // Arrange
        var xmlContent = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.2.3</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;

        // Act
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Assert
        Assert.NotNull(doc);
        Assert.Equal("1.2.3", doc.GetVersion("CoreLib"));
        Assert.False(doc.Changed);
    }

    [Fact]
    public void Parse_ShouldThrowInvalidDataException_WhenPropertyGroupIsMissing()
    {
        // Arrange
        var invalidXml = "<Project></Project>";

        // Act & Assert
        _ = Assert.Throws<InvalidDataException>(() => VersionsPropsDocument.Parse(invalidXml));
    }

    [Fact]
    public void LoadOrCreate_ShouldCreateVirtualEmptyDocument_WhenFileDoesNotExist()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var nonExistentPath = ws.RootDirectory / "Versions.props";

        // Act
        var doc = VersionsPropsDocument.LoadOrCreate(nonExistentPath);

        // Assert
        Assert.NotNull(doc);
        Assert.False(doc.Changed);
        Assert.Null(doc.GetVersion("NonExistentProject"));
    }

    [Fact]
    public async Task LoadOrCreate_ShouldLoadExistingFile_WhenFileExistsOnDisk()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var filePath = ws.RootDirectory / "Versions.props";
        var initialXml = """
            <Project>
              <PropertyGroup>
                <AppLibVersion>2.0.0</AppLibVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateTextFileAsync("Versions.props", initialXml);

        // Act
        var doc = VersionsPropsDocument.LoadOrCreate(filePath);

        // Assert
        Assert.NotNull(doc);
        Assert.Equal("2.0.0", doc.GetVersion("AppLib"));
        Assert.False(doc.Changed);
    }

    #endregion

    #region Read & TryGetVersion Tests

    [Fact]
    public void TryGetVersion_ShouldReturnTrueAndVersion_WhenProjectExists()
    {
        // Arrange
        var xmlContent = """
            <Project>
              <PropertyGroup>
                <MyProjectVersion>1.0.0</MyProjectVersion>
              </PropertyGroup>
            </Project>
            """;
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Act
        var found = doc.TryGetVersion("My.Project", out var version);

        // Assert
        Assert.True(found);
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public void TryGetVersion_ShouldReturnFalseAndNull_WhenProjectDoesNotExist()
    {
        // Arrange
        var xmlContent = "<Project><PropertyGroup /></Project>";
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Act
        var found = doc.TryGetVersion("UnknownProject", out var version);

        // Assert
        Assert.False(found);
        Assert.Null(version);
    }

    #endregion

    #region Mutation Tests (TrySetVersion & RemoveVersion)

    [Fact]
    public void TrySetVersion_ShouldAddNewVersion_AndSetChangedFlagToTrue()
    {
        // Arrange
        var doc = VersionsPropsDocument.Parse("<Project><PropertyGroup /></Project>");

        // Act
        var resultSet = doc.TrySetVersion("CoreLib", "1.0.0");

        // Assert
        Assert.True(resultSet);
        Assert.True(doc.Changed);
        Assert.Equal("1.0.0", doc.GetVersion("CoreLib"));
    }

    [Fact]
    public void TrySetVersion_ShouldUpdateExistingVersion_WhenNewValueIsDifferent()
    {
        // Arrange
        var xmlContent = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Act
        var resultSet = doc.TrySetVersion("CoreLib", "1.1.0");

        // Assert
        Assert.True(resultSet);
        Assert.True(doc.Changed);
        Assert.Equal("1.1.0", doc.GetVersion("CoreLib"));
    }

    [Fact]
    public void TrySetVersion_ShouldReturnFalse_WhenNewValueIsIdenticalToCurrent()
    {
        // Arrange
        var xmlContent = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Act
        var resultSet = doc.TrySetVersion("CoreLib", "1.0.0");

        // Assert
        Assert.False(resultSet);
        Assert.False(doc.Changed);
    }

    [Theory]
    [InlineData(null, "1.0.0")]
    [InlineData("", "1.0.0")]
    [InlineData("   ", "1.0.0")]
    [InlineData("CoreLib", null)]
    [InlineData("CoreLib", "")]
    [InlineData("CoreLib", "   ")]
    public void TrySetVersion_ShouldThrowArgumentException_WhenInputsAreNullOrWhitespace(string? projectName, string? version)
    {
        // Arrange
        var doc = VersionsPropsDocument.Parse("<Project><PropertyGroup /></Project>");

        // Act & Assert: Use ThrowsAny to accept both ArgumentException (for whitespace) and derived ArgumentNullException (for null)
        _ = Assert.ThrowsAny<ArgumentException>(() => doc.TrySetVersion(projectName!, version!));
    }

    [Fact]
    public void RemoveVersion_ShouldRemoveElement_AndSetChangedFlagToTrue()
    {
        // Arrange
        var xmlContent = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Act
        var resultRemove = doc.RemoveVersion("CoreLib");

        // Assert
        Assert.True(resultRemove);
        Assert.True(doc.Changed);
        Assert.Null(doc.GetVersion("CoreLib"));
    }

    [Fact]
    public void RemoveVersion_ShouldReturnFalse_WhenElementDoesNotExist()
    {
        // Arrange
        var doc = VersionsPropsDocument.Parse("<Project><PropertyGroup /></Project>");

        // Act
        var resultRemove = doc.RemoveVersion("NonExistent");

        // Assert
        Assert.False(resultRemove);
        Assert.False(doc.Changed);
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Indexer_ShouldGetAndSetVersionCorrectly()
    {
        // Arrange
        var doc = VersionsPropsDocument.Parse("<Project><PropertyGroup /></Project>");

        // Act
        doc["My.Service"] = "3.2.1";

        // Assert
        Assert.Equal("3.2.1", doc["My.Service"]);
        Assert.True(doc.Changed);
    }

    [Fact]
    public void Indexer_ShouldRemoveVersion_WhenValueIsSetToNullOrWhitespace()
    {
        // Arrange
        var xmlContent = """
            <Project>
              <PropertyGroup>
                <MyServiceVersion>3.2.1</MyServiceVersion>
              </PropertyGroup>
            </Project>
            """;
        var doc = VersionsPropsDocument.Parse(xmlContent);

        // Act
        doc["MyService"] = null;

        // Assert
        Assert.Null(doc["MyService"]);
        Assert.True(doc.Changed);
    }

    #endregion

    #region Persistence Tests (Save)

    [Fact]
    public void Save_ShouldPersistDocumentToDisk_AndResetChangedFlag()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var filePath = ws.RootDirectory / "Versions.props";
        var doc = VersionsPropsDocument.Parse("<Project><PropertyGroup /></Project>");

        doc.TrySetVersion("CoreLib", "1.0.0");
        Assert.True(doc.Changed);

        // Act
        doc.Save(filePath);

        // Assert
        Assert.False(doc.Changed);
        Assert.True(File.Exists(filePath));

        var reloadedDoc = VersionsPropsDocument.LoadOrCreate(filePath);
        Assert.Equal("1.0.0", reloadedDoc.GetVersion("CoreLib"));
    }

    #endregion
}
