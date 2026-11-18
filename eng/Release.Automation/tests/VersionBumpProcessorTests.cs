using Microsoft.Extensions.Logging.Abstractions;

using Nuke.Common.IO;

namespace Release.Automation.Tests;

/// <summary>
/// Contains unit and integration tests for <see cref="VersionBumpProcessor"/>.
/// </summary>
public class VersionBumpProcessorTests
{
    static readonly NullLogger Logger = NullLogger.Instance;

    #region Unit Tests: GetChangedPackageNames

    [Theory]
    [InlineData("+ <PackageVersion Include=\"Newtonsoft.Json\" Version=\"13.0.3\" />", "Newtonsoft.Json")]
    [InlineData("- <PackageVersion Include=\"Microsoft.Extensions.Logging\" Version=\"8.0.0\" />", "Microsoft.Extensions.Logging")]
    [InlineData("  <PackageVersion Include=\"Unchanged.Package\" Version=\"1.0.0\" />", null)]
    public void GetChangedPackageNames_ShouldExtractPackageNamesFromDiffText(string diffLine, string? expectedPackage)
    {
        // Act
        var result = VersionBumpProcessor.GetChangedPackageNames(diffLine);

        // Assert
        if (expectedPackage is null)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Contains(expectedPackage, result);
        }
    }

    #endregion

    #region Integration Tests: Execute Pipeline

    [Fact]
    public async Task Execute_ShouldDetectNewProject_AndInitializeVersionInVersionsProps()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var csprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/NewLib/NewLib.csproj", csprojContent);
        var solution = await ws.CreateSolutionAsync([("NewLib", "src/NewLib/NewLib.csproj")]);

        await ws.CommitAllAsync("Add project files");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        Assert.Single(activeProjects);
        var ctx = activeProjects[0];
        Assert.True(ctx.IsNewProject);
        Assert.Equal("0.1.0", ctx.CalculatedVersion);

        // Verify Versions.props was written to disk
        var versionsPropsPath = ws.RootDirectory / "Versions.props";
        Assert.True(File.Exists(versionsPropsPath));
        var doc = VersionsPropsDocument.LoadOrCreate(versionsPropsPath);
        Assert.Equal("0.1.0", doc.GetVersion("NewLib"));
    }

    [Fact]
    public async Task Execute_ShouldTriggerCascadePatch_WhenDependencyHasMajorBump()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var appCsprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../CoreLib/CoreLib.csproj" />
              </ItemGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/AppLib/AppLib.csproj", appCsprojContent);

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <AppLibVersion>1.0.0</AppLibVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0", "AppLib-v1.0.0");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj"), ("AppLib", "src/AppLib/AppLib.csproj")]);

        // Commit breaking change (!:) in CoreLib
        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Breaking API change");
        await ws.CommitAllAsync("feat(CoreLib)!: breaking change");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        var coreCtx = activeProjects.First(p => p.Project.Name == "CoreLib");
        var appCtx = activeProjects.First(p => p.Project.Name == "AppLib");

        Assert.Equal(VersionBumpType.Major, coreCtx.BumpType);
        Assert.Equal(VersionBumpType.Patch, appCtx.BumpType);
        Assert.True(appCtx.HasSecurityFix);
    }

    [Fact]
    public async Task Execute_ShouldApplyForceProject_OnlyWhenDependencyHasUpdated()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var appCsprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>true</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../CoreLib/CoreLib.csproj" />
              </ItemGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/AppLib/AppLib.csproj", appCsprojContent);

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <AppLibVersion>1.0.0</AppLibVersion>
              </PropertyGroup>
            </Project>
            """;

        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0", "AppLib-v1.0.0");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj"), ("AppLib", "src/AppLib/AppLib.csproj")]);

        await ws.CommitAllAsync("Initial project setup");

        // Act: Force publish AppLib when no dependencies have changed
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: ["AppLib"],
            logger: Logger);

        // Assert: AppLib should be skipped because its dependencies didn't change
        Assert.Empty(activeProjects);
    }

    #endregion

    #region Integration Tests: Breaking Change Commits

    [Fact]
    public async Task Execute_ShouldAssignMajorBump_WhenCommitHeaderContainsExclamationPoint()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;

        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);

        // Commit header with '!:' breaking change marker
        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Updated signature");
        await ws.CommitAllAsync("refactor(CoreLib)!: redesign core interfaces");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        var coreCtx = Assert.Single(activeProjects);
        Assert.Equal(VersionBumpType.Major, coreCtx.BumpType);
        Assert.Equal("2.0.0", coreCtx.CalculatedVersion);
    }

    [Fact]
    public async Task Execute_ShouldAssignMajorBump_WhenCommitFooterContainsBreakingChangeText()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;

        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);

        // Commit with BREAKING CHANGE text in message body/footer
        var commitMessage = """
            feat(CoreLib): remove legacy serialization method

            BREAKING CHANGE: Removed SerializeToXml method from CoreLib API.
            """;

        await ws.CreateTextFileAsync("src/CoreLib/Serializer.cs", "// Removed legacy method");
        await ws.CommitAllAsync(commitMessage);

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        var coreCtx = Assert.Single(activeProjects);
        Assert.Equal(VersionBumpType.Major, coreCtx.BumpType);
        Assert.Equal("2.0.0", coreCtx.CalculatedVersion);
    }

    [Fact]
    public async Task Execute_ShouldAssignMinorBump_WhenCommitIsStandardFeatureWithoutBreakingChange()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;

        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);

        // Standard feature commit without breaking change
        await ws.CreateTextFileAsync("src/CoreLib/NewFeature.cs", "// Non-breaking additive change");
        await ws.CommitAllAsync("feat(CoreLib): add new helper methods");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        var coreCtx = Assert.Single(activeProjects);
        Assert.Equal(VersionBumpType.Minor, coreCtx.BumpType);
        Assert.Equal("1.1.0", coreCtx.CalculatedVersion);
    }

    [Fact]
    public async Task Execute_ShouldTriggerCascadePatch_WhenDependencyHasBreakingChangeFooter()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var appCsprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../CoreLib/CoreLib.csproj" />
              </ItemGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/AppLib/AppLib.csproj", appCsprojContent);

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <AppLibVersion>1.0.0</AppLibVersion>
              </PropertyGroup>
            </Project>
            """;

        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0", "AppLib-v1.0.0");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj"), ("AppLib", "src/AppLib/AppLib.csproj")]);

        // Breaking change via footer in CoreLib
        var commitMessage = """
            fix(CoreLib): alter public behavior

            BREAKING-CHANGE: Return value format has changed.
            """;

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Updated implementation");
        await ws.CommitAllAsync(commitMessage);

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        var coreCtx = activeProjects.First(p => p.Project.Name == "CoreLib");
        var appCtx = activeProjects.First(p => p.Project.Name == "AppLib");

        Assert.Equal(VersionBumpType.Major, coreCtx.BumpType);
        Assert.Equal(VersionBumpType.Patch, appCtx.BumpType);
        Assert.True(appCtx.HasSecurityFix);
    }

    #endregion

    [Fact]
    public async Task Execute_ShouldPropagateCascadePatch_ThroughDeepDependencyChain()
    {
        // Arrange (Graph: CoreLib -> ServiceLib -> AppHost)
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // 1. Create projects
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        _ = await ws.CreateTextFileAsync("src/ServiceLib/ServiceLib.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../CoreLib/CoreLib.csproj" />
              </ItemGroup>
            </Project>
            """);

        _ = await ws.CreateTextFileAsync("src/AppHost/AppHost.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../ServiceLib/ServiceLib.csproj" />
              </ItemGroup>
            </Project>
            """);

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <ServiceLibVersion>1.0.0</ServiceLibVersion>
                <AppHostVersion>1.0.0</AppHostVersion>
              </PropertyGroup>
            </Project>
            """;

        // 2. Add base tags
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml,
            "CoreLib-v1.0.0", "ServiceLib-v1.0.0", "AppHost-v1.0.0");

        var solution = await ws.CreateSolutionAsync([
            ("CoreLib", "src/CoreLib/CoreLib.csproj"),
            ("ServiceLib", "src/ServiceLib/ServiceLib.csproj"),
            ("AppHost", "src/AppHost/AppHost.csproj")
        ]);

        // 3. Major commit (!:) for CoreLib
        await ws.CreateTextFileAsync("src/CoreLib/CoreLibApi.cs", "// Breaking API change");
        await ws.CommitAllAsync("feat(CoreLib)!: breaking change in core infrastructure");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        Assert.Equal(3, activeProjects.Count);

        var coreCtx = activeProjects.First(p => p.Project.Name == "CoreLib");
        var serviceCtx = activeProjects.First(p => p.Project.Name == "ServiceLib");
        var appHostCtx = activeProjects.First(p => p.Project.Name == "AppHost");

        // CoreLib -> Major
        Assert.Equal(VersionBumpType.Major, coreCtx.BumpType);
        Assert.Equal("2.0.0", coreCtx.CalculatedVersion);

        // ServiceLib -> Cascade Patch (from CoreLib)
        Assert.Equal(VersionBumpType.Patch, serviceCtx.BumpType);
        Assert.Equal("1.0.1", serviceCtx.CalculatedVersion);

        // AppHost -> Cascade Patch (from ServiceLib)
        Assert.Equal(VersionBumpType.Patch, appHostCtx.BumpType);
        Assert.Equal("1.0.1", appHostCtx.CalculatedVersion);
    }

    [Fact]
    public async Task Execute_ShouldHandleDiamondDependencyGraph_WithoutDuplicates()
    {
        // Arrange (Diamond Graph: CoreLib <- ServiceA & ServiceB <- AppHost)
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        _ = await ws.CreateTextFileAsync("src/ServiceA/ServiceA.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../CoreLib/CoreLib.csproj" />
              </ItemGroup>
            </Project>
            """);

        _ = await ws.CreateTextFileAsync("src/ServiceB/ServiceB.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../CoreLib/CoreLib.csproj" />
              </ItemGroup>
            </Project>
            """);

        _ = await ws.CreateTextFileAsync("src/AppHost/AppHost.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../ServiceA/ServiceA.csproj" />
                <ProjectReference Include="../ServiceB/ServiceB.csproj" />
              </ItemGroup>
            </Project>
            """);

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <ServiceAVersion>1.0.0</ServiceAVersion>
                <ServiceBVersion>1.0.0</ServiceBVersion>
                <AppHostVersion>1.0.0</AppHostVersion>
              </PropertyGroup>
            </Project>
            """;

        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml,
            "CoreLib-v1.0.0", "ServiceA-v1.0.0", "ServiceB-v1.0.0", "AppHost-v1.0.0");

        var solution = await ws.CreateSolutionAsync([
            ("CoreLib", "src/CoreLib/CoreLib.csproj"),
            ("ServiceA", "src/ServiceA/ServiceA.csproj"),
            ("ServiceB", "src/ServiceB/ServiceB.csproj"),
            ("AppHost", "src/AppHost/AppHost.csproj")
        ]);

        await ws.CreateTextFileAsync("src/CoreLib/IContract.cs", "// Updated Interface");
        await ws.CommitAllAsync("feat(CoreLib)!: update shared interfaces");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        Assert.Equal(4, activeProjects.Count);

        var coreCtx = activeProjects.First(p => p.Project.Name == "CoreLib");
        var serviceACtx = activeProjects.First(p => p.Project.Name == "ServiceA");
        var serviceBCtx = activeProjects.First(p => p.Project.Name == "ServiceB");
        var appHostCtx = activeProjects.First(p => p.Project.Name == "AppHost");

        Assert.Equal(VersionBumpType.Major, coreCtx.BumpType);
        Assert.Equal(VersionBumpType.Patch, serviceACtx.BumpType);
        Assert.Equal(VersionBumpType.Patch, serviceBCtx.BumpType);

        Assert.Equal(VersionBumpType.Patch, appHostCtx.BumpType);
        Assert.Equal("1.0.1", appHostCtx.CalculatedVersion);
    }

    #region Integration Tests: Central Package Management (CPM)

    [Fact]
    public async Task Execute_ShouldTriggerBump_WhenPackageInDirectoryPackagesPropsIsUpdatedAndReferencedByProject()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var csprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="12.0.1" />
              </ItemGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", csprojContent);

        var baselinePackages = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="12.0.1" />
              </ItemGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Directory.Packages.props", baselinePackages, "CoreLib-v1.0.0");

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateTextFileAsync("Versions.props", baselineVersionsXml);
        await ws.CommitAllAsync("Add baseline versions");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);

        // Bump Newtonsoft.Json version in Directory.Packages.props
        var updatedPackages = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """;
        await ws.CreateTextFileAsync("Directory.Packages.props", updatedPackages);
        await ws.CommitAllAsync("cpm: update Newtonsoft.Json to 13.0.3");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert
        var coreCtx = Assert.Single(activeProjects);
        Assert.Equal(VersionBumpType.Patch, coreCtx.BumpType);
        Assert.Equal("1.0.1", coreCtx.CalculatedVersion);
    }

    [Fact]
    public async Task Execute_ShouldIgnoreDirectoryPackagesPropsChanges_WhenUpdatedPackageIsNotReferencedByProject()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var csprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Serilog" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", csprojContent);

        var baselinePackages = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="12.0.1" />
              </ItemGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Directory.Packages.props", baselinePackages, "CoreLib-v1.0.0");

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateTextFileAsync("Versions.props", baselineVersionsXml);
        await ws.CommitAllAsync("Add baseline versions");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);

        // Update Newtonsoft.Json which CoreLib does NOT reference
        var updatedPackages = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """;
        await ws.CreateTextFileAsync("Directory.Packages.props", updatedPackages);
        await ws.CommitAllAsync("cpm: update Newtonsoft.Json to 13.0.3");

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert: Project should be skipped since it doesn't reference the updated package
        Assert.Empty(activeProjects);
    }

    #endregion

    #region Integration Tests: PublicAPI Promotion & Release Channels

    [Fact]
    public async Task Execute_ShouldPromotePublicApi_OnlyWhenChannelIsStable()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj",
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>");

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        // Create baseline tag commit
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "CoreLib-v1.0.0");

        // Add unshipped API changes AFTER the baseline tag commit
        var unshippedContent = """
            #nullable enable
            CoreLib.ClassA.NewApiMethod() -> void
            """;
        await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        await ws.CommitAllAsync("Add unshipped public API");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);

        // Act 1: Execute for Beta channel (Pre-release)
        var betaProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Beta,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert 1: PublicAPI should NOT be promoted on Beta channel
        var shippedPath = ws.RootDirectory / "src/CoreLib/PublicAPI.Shipped.txt";
        Assert.False(File.Exists(shippedPath));
        Assert.Single(betaProjects);

        // Act 2: Execute for Stable channel
        var stableProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert 2: PublicAPI SHOULD be promoted on Stable channel
        Assert.Single(stableProjects);
        Assert.True(File.Exists(shippedPath));
        var shippedLines = await File.ReadAllLinesAsync(shippedPath);
        Assert.Contains("CoreLib.ClassA.NewApiMethod() -> void", shippedLines);
    }

    #endregion

    #region Integration Tests: Filtering & Initialization

    [Fact]
    public async Task Execute_ShouldIgnoreProjects_WithoutPackableProperties()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // Project without IsPackable or GeneratePackageOnBuild
        var internalCsproj = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/InternalTools/InternalTools.csproj", internalCsproj);
        await ws.CommitAllAsync("Add internal non-packable tool");

        var solution = await ws.CreateSolutionAsync([("InternalTools", "src/InternalTools/InternalTools.csproj")]);

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: false,
            forceProjects: [],
            logger: Logger);

        // Assert: Non-packable project should be ignored
        Assert.Empty(activeProjects);
    }

    [Fact]
    public async Task Execute_ShouldInitializeNewProjectAs100_WhenPromoteToMajorIsTrueAndChannelIsExplicit()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var csprojContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><IsPackable>true</IsPackable></PropertyGroup>
            </Project>
            """;
        _ = await ws.CreateTextFileAsync("src/NewLib/NewLib.csproj", csprojContent);
        await ws.CommitAllAsync("Add new library");

        var solution = await ws.CreateSolutionAsync([("NewLib", "src/NewLib/NewLib.csproj")]);

        // Act
        var activeProjects = VersionBumpProcessor.Execute(
            [.. solution.AllProjects],
            ws.RootDirectory,
            versionsPropsFile: (RelativePath)"Versions.props",
            channel: ReleaseChannel.Stable,
            promoteToMajor: true,
            forceProjects: [],
            logger: Logger);

        // Assert: Initial version should be 1.0.0 instead of default 0.1.0
        var ctx = Assert.Single(activeProjects);
        Assert.True(ctx.IsNewProject);
        Assert.Equal("1.0.0", ctx.CalculatedVersion);
    }

    #endregion
}
