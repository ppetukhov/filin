using System.Collections.Immutable;

namespace Filin.Abstractions.ProjectorTests;

[ProjectorId("some-projector")]
sealed partial class SomeProjector : IProjector<SomeProjState>
{
    public static ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static DcbQuery GetQuery(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static SomeProjState Project(SomeProjState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static SomeProjState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

[ProjectorId("some-projector-with-default-ver", 0)]
sealed partial class SomeProjectorWithDefaultVersion : IProjector<SomeProjDefVerState>
{
    public static ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static DcbQuery GetQuery(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static SomeProjDefVerState Project(SomeProjDefVerState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static SomeProjDefVerState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

[ProjectorId("some-projector", 1)]
sealed partial class SomeProjectorV1 : IProjector<SomeProjV1State>
{
    public static ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static DcbQuery GetQuery(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static SomeProjV1State Project(SomeProjV1State state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static SomeProjV1State Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

[ProjectorId("some-projector", 2025_05)]
sealed partial record SomeProjectorV2025_05 : IProjector<SomeProjV2025_05State>
{
    public static ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static DcbQuery GetQuery(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
    public static SomeProjV2025_05State Project(SomeProjV2025_05State state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static SomeProjV2025_05State Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

sealed partial class TestDomainEvents
{
    internal static partial class V0
    {
        [ProjectorId("some-nested-projector")]
        internal sealed partial record SomeNestedProjector : IProjector<SomeProjNestedState>
        {
            public static ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
            public static DcbQuery GetQuery(ImmutableHashSet<Tag> tags) => throw new NotImplementedException();
            public static SomeProjNestedState Project(SomeProjNestedState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
            public static SomeProjNestedState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
        }
    }
}

sealed record SomeProjState { };
sealed record SomeProjDefVerState { };
sealed record SomeProjV1State { };
sealed record SomeProjV2025_05State { };
sealed record SomeProjNestedState { };

public sealed class ProjectorIdAttributeTests
{
    [Fact]
    public void SomeProjector_Propery_ProjectorId_Equal_Some_Projector()
    {
        SomeProjector.ProjectorId.Should().Be(new ProjectorId("some-projector"));
    }

    [Fact]
    public void SomeNestedProjector_Propery_ProjectorId_Equal_SomeNestedProjector()
    {
        TestDomainEvents.V0.SomeNestedProjector.ProjectorId.Should().Be(new ProjectorId("some-nested-projector"));
    }

    [Fact]
    public void SomeProjectorWithDefaultVersion_Propery_ProjectorId_Without_Version()
    {
        SomeProjectorWithDefaultVersion.ProjectorId.Should().Be(new ProjectorId("some-projector-with-default-ver"));
    }

    [Fact]
    public void SomeProjectorV1_Propery_ProjectorId_With_VersionAt1()
    {
        SomeProjectorV1.ProjectorId.Should().Be(new ProjectorId("some-projector@1"));
    }

    [Fact]
    public void SomeProjectorV2025_05_Propery_ProjectorId_With_VersionAt202505()
    {
        SomeProjectorV2025_05.ProjectorId.Should().Be(new ProjectorId("some-projector@202505"));
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectorTypes_TotalCount()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new MetadataProjectors_Filin_Abstractions_Tests();

        provider.Configure(options);

        options.ProjectorTypes.Should().HaveCount(11);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectionTypes_TotalCount()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new MetadataProjectors_Filin_Abstractions_Tests();

        provider.Configure(options);

        options.ProjectionTypes.Should().HaveCount(11);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectorTypes_SomeProjector()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new MetadataProjectors_Filin_Abstractions_Tests();

        provider.Configure(options);

        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(SomeProjState), typeof(SomeProjector)));
        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(SomeProjDefVerState), typeof(SomeProjectorWithDefaultVersion)));
        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(SomeProjV1State), typeof(SomeProjectorV1)));
        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(SomeProjV2025_05State), typeof(SomeProjectorV2025_05)));
        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(SomeProjNestedState), typeof(TestDomainEvents.V0.SomeNestedProjector)));
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectionTypes_SomeProjector()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new MetadataProjectors_Filin_Abstractions_Tests();

        provider.Configure(options);

        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("some-projector", typeof(SomeProjState)));
        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("some-projector-with-default-ver", typeof(SomeProjDefVerState)));
        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("some-projector@1", typeof(SomeProjV1State)));
        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("some-projector@202505", typeof(SomeProjV2025_05State)));
        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("some-nested-projector", typeof(SomeProjNestedState)));
    }
}
