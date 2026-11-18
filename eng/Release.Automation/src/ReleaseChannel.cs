namespace Release.Automation;

enum ReleaseChannel
{
    Alpha,
    Beta,
    Rc,
    Stable
}

static class ReleaseChannelExtensions
{
    extension(ReleaseChannel channel)
    {
        public bool IsExplicitRelease => channel != ReleaseChannel.Alpha;
    }
}
