using Nuke.Common.IO;

namespace Release.Automation;

/// <summary>
/// Provides logic for calculating semantic versions across different release channels.
/// </summary>
static class VersionCalculator
{
    public static string CalculateNextVersion(
        AbsolutePath rootDirectory,
        string currentVersionString,
        VersionBumpType bumpType,
        ReleaseChannel channel,
        bool isNewProject,
        bool promoteToMajor)
    {
        var parts = currentVersionString.Split('-', 2);
        var baseVersion = Version.Parse(parts[0]);
        var existingSuffix = parts.Length > 1 ? parts[1] : null;

        var major = baseVersion.Major;
        var minor = baseVersion.Minor;
        var patch = baseVersion.Build < 0 ? 0 : baseVersion.Build;

        if (channel == ReleaseChannel.Alpha)
        {
            if (!isNewProject && existingSuffix is null)
            {
                ApplySemVerBump(ref major, ref minor, ref patch, bumpType, promoteToMajor);
            }

            var commitCount = GitTasksHelper.GetCommitCount(rootDirectory);

            return $"{major}.{minor}.{patch}-alpha.{commitCount}";
        }

#pragma warning disable CA1308 // Normalize strings to uppercase
        var targetChannelName = channel.ToString().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

        if (isNewProject)
        {
            var startBase = promoteToMajor ? "1.0.0" : "0.1.0";
            return channel == ReleaseChannel.Stable ? startBase : $"{startBase}-{targetChannelName}.1";
        }

        if (existingSuffix is not null)
        {
            if (channel == ReleaseChannel.Stable)
            {
                return major == 0 && promoteToMajor ? "1.0.0" : $"{major}.{minor}.{patch}";
            }

            var suffixParts = existingSuffix.Split('.');

            return suffixParts.Length == 2
                && string.Equals(suffixParts[0], targetChannelName, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(suffixParts[1], out var preReleaseNumber)
                    ? $"{major}.{minor}.{patch}-{targetChannelName}.{preReleaseNumber + 1}"
                    : $"{major}.{minor}.{patch}-{targetChannelName}.1";
        }

        ApplySemVerBump(ref major, ref minor, ref patch, bumpType, promoteToMajor);

        return channel == ReleaseChannel.Stable
            ? $"{major}.{minor}.{patch}"
            : $"{major}.{minor}.{patch}-{targetChannelName}.1";
    }

    static void ApplySemVerBump(
        ref int major,
        ref int minor,
        ref int patch,
        VersionBumpType bumpType,
        bool promoteToMajor)
    {
        if (major == 0 && promoteToMajor)
        {
            major = 1;
            minor = 0;
            patch = 0;
            return;
        }

        if (major == 0)
        {
            switch (bumpType)
            {
                case VersionBumpType.Major:
                    minor++;
                    patch = 0;
                    break;
                case VersionBumpType.Minor:
                case VersionBumpType.Patch:
                    patch++;
                    break;
                case VersionBumpType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(bumpType), bumpType, "Unsupported version bump type.");
            }

            return;
        }

        switch (bumpType)
        {
            case VersionBumpType.Major:
                major++;
                minor = 0;
                patch = 0;
                break;
            case VersionBumpType.Minor:
                minor++;
                patch = 0;
                break;
            case VersionBumpType.Patch:
                patch++;
                break;
            case VersionBumpType.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(bumpType), bumpType, "Unsupported version bump type.");
        }
    }
}
