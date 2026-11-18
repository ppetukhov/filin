using Microsoft.Extensions.Logging;

namespace Release.Automation;

static partial class BuildLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "[NUKE] No changes detected for '{ProjectName}'. Skipping.")]
    internal static partial void ProjectSkippedNoChanges(this ILogger logger, string projectName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "[NUKE] New NuGet project '{ProjectName}' detected with {InitialVersion}")]
    internal static partial void NewProjectDetected(
        this ILogger logger,
        string projectName,
        string initialVersion);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "[{Channel} Release] {ProjectName}: {CurrentVersion} -> {NewVersion} (Reason: {BumpType})")]
    internal static partial void ReleaseBump(
        this ILogger logger,
        string channel,
        string projectName,
        string currentVersion,
        string newVersion,
        string bumpType);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "[NUKE] Behavioral breaking change marker (!:) detected in commits for '{ProjectName}'. Forcing Major bump.")]
    internal static partial void BehavioralBreakingChangeDetected(this ILogger logger, string projectName);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "[NUKE] Central Versions.props file has been saved.")]
    internal static partial void VersionsPropsSaved(this ILogger logger);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "[Git] Creating and pushing tag {TagName}")]
    internal static partial void CreatingGitTag(this ILogger logger, string tagName);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "[Git] Failed to create tag {TagName}. Error: {Message}")]
    internal static partial void GitTagCreationFailed(this ILogger logger, string tagName, string message);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "[NUKE] Versions.props not found. Using virtual XML in memory.")]
    internal static partial void WorkingWithVirtualXmlStructure(this ILogger logger);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "[PublicAPI] Promoted Unshipped API changes to PublicAPI.Shipped.txt for '{ProjectName}'.")]
    internal static partial void PromotedPublicApi(this ILogger logger, string projectName);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "[NUKE] Cascade Patch triggered for '{ProjectName}' due to a Major or Security update in its dependencies.")]
    internal static partial void CascadeDependencyBump(this ILogger logger, string projectName);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "[NUKE] Forced release accepted for '{ProjectName}' because one or more dependencies have newer versions.")]
    internal static partial void ForcedReleaseAccepted(this ILogger logger, string projectName);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "[NUKE] Idle release prevented for '{ProjectName}'. Neither its code nor any of its dependencies changed since last release.")]
    internal static partial void IdleForcedReleaseSkipped(this ILogger logger, string projectName);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Information,
        Message = "[CPM] Project '{ProjectName}' references updated package '{PackageName}' from Directory.Packages.props.")]
    internal static partial void CentralPackageBump(
        this ILogger logger,
        string projectName,
        string packageName);
}
