namespace Filin.Abstractions.ProjectorTests;

[ProjectorId("projector-without-tags")]
[QueryEventTypes("CourseCreated")]
[QueryEventTypes("CourseRenamed", "CourseScheduled")]
[QueryEventTypes(0, "CourseCapacityChanged", "CourseStarted")]
[QueryEventTypes(100, "CourseFinished")]
[QueryEventTypes(-1, "StudentSubscribed")]
sealed partial class ProjectorWithoutQueryTags : IProjector<ProjState>
{
    public static ProjState Project(ProjState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static ProjState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

[ProjectorId("projector-without-tags", 1)]
[QueryEventTypes("CourseCreated")]
[QueryEventTypes("CourseRenamed", "CourseScheduled")]
[QueryEventTypes(0, "CourseCapacityChanged", "CourseStarted")]
[QueryEventTypes(100, "CourseFinished")]
[QueryEventTypes(-1, "StudentSubscribed")]
sealed partial class ProjectorWithoutQueryTagsV1 : IProjector<ProjV1State>
{
    public static ProjV1State Project(ProjV1State state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static ProjV1State Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

sealed partial class TestDomainProjectors
{
    internal static partial class V0
    {
        [ProjectorId("nested-projector-without-tags")]
        [QueryEventTypes("CourseCreated")]
        [QueryEventTypes("CourseRenamed", "CourseScheduled")]
        [QueryEventTypes(0, "CourseCapacityChanged", "CourseStarted")]
        [QueryEventTypes(100, "CourseFinished")]
        [QueryEventTypes(-1, "StudentSubscribed")]
        internal sealed partial record NestedProjectorWithoutQueryTags : IProjector<ProjNestedState>
        {
            public static ProjNestedState Project(ProjNestedState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
            public static ProjNestedState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
        }
    }
}

sealed record ProjState { };
sealed record ProjV1State { };
sealed record ProjNestedState { };

public sealed class QueryEventTypesAttributeTests
{
    [Fact]
    public void ProjectorWithoutQueryTags_GetProjectionId_Return_ProjectionIdWithDefaultKey()
    {
        var result = ProjectorWithoutQueryTags.GetProjectionId([]);

        var defaultKey = (IdSpan)default;
        var expected = new ProjectionId(new ProjectorId("projector-without-tags"), defaultKey);

        result.Should().Be(expected);
    }

    [Fact]
    public void ProjectorWithoutQueryTagsV1_GetProjectionId_Return_ProjectionId_Version1()
    {
        var result = ProjectorWithoutQueryTagsV1.GetProjectionId([]);

        var defaultKey = (IdSpan)default;
        var expected = new ProjectionId(new ProjectorId("projector-without-tags@1"), defaultKey);

        result.Should().Be(expected);
    }

    [Fact]
    public void NestedProjectorWithoutQueryTags_GetProjectionId_ProjectionIdWithDefaultKey()
    {
        var result = TestDomainProjectors.V0.NestedProjectorWithoutQueryTags.GetProjectionId([]);

        var defaultKey = (IdSpan)default;
        var expected = new ProjectionId(new ProjectorId("nested-projector-without-tags"), defaultKey);

        result.Should().Be(expected);
    }

    [Fact]
    public void ProjectorWithoutQueryTags_GetQuery_Return_QueryWithThreeItems()
    {
        var result = ProjectorWithoutQueryTags.GetQuery([]);

        var expected1 = new DcbQueryItem(["StudentSubscribed"], []);
        var expected2 = new DcbQueryItem(["CourseCreated", "CourseRenamed", "CourseScheduled", "CourseCapacityChanged", "CourseStarted"], []);
        var expected3 = new DcbQueryItem(["CourseFinished"], []);

        result.Items.Should().HaveCount(3);

        result.Items.Should().ContainEquivalentOf(expected1);
        result.Items.Should().ContainEquivalentOf(expected2);
        result.Items.Should().ContainEquivalentOf(expected3);
    }

    [Fact]
    public void ProjectorWithoutQueryTagsV1_GetQuery_Return_QueryWithThreeItems()
    {
        var result = ProjectorWithoutQueryTagsV1.GetQuery([]);

        var expected1 = new DcbQueryItem(["StudentSubscribed"], []);
        var expected2 = new DcbQueryItem(["CourseCreated", "CourseRenamed", "CourseScheduled", "CourseCapacityChanged", "CourseStarted"], []);
        var expected3 = new DcbQueryItem(["CourseFinished"], []);

        result.Items.Should().HaveCount(3);

        result.Items.Should().ContainEquivalentOf(expected1);
        result.Items.Should().ContainEquivalentOf(expected2);
        result.Items.Should().ContainEquivalentOf(expected3);
    }

    [Fact]
    public void NestedProjectorWithoutQueryTags_GetQuery_Return_QueryWithThreeItems()
    {
        var result = TestDomainProjectors.V0.NestedProjectorWithoutQueryTags.GetQuery([]);

        var expected1 = new DcbQueryItem(["StudentSubscribed"], []);
        var expected2 = new DcbQueryItem(["CourseCreated", "CourseRenamed", "CourseScheduled", "CourseCapacityChanged", "CourseStarted"], []);
        var expected3 = new DcbQueryItem(["CourseFinished"], []);

        result.Items.Should().HaveCount(3);

        result.Items.Should().ContainEquivalentOf(expected1);
        result.Items.Should().ContainEquivalentOf(expected2);
        result.Items.Should().ContainEquivalentOf(expected3);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectorTypes_ProjectorWithoutQueryTags()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new MetadataProjectors_Filin_Abstractions_Tests();

        provider.Configure(options);

        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(ProjState), typeof(ProjectorWithoutQueryTags)));
        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(ProjV1State), typeof(ProjectorWithoutQueryTagsV1)));
        options.ProjectorTypes.Should().ContainEquivalentOf<KeyValuePair<Type, Type>>(new(typeof(ProjNestedState), typeof(TestDomainProjectors.V0.NestedProjectorWithoutQueryTags)));
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectionTypes_ProjectorWithoutQueryTags()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new MetadataProjectors_Filin_Abstractions_Tests();

        provider.Configure(options);

        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("projector-without-tags", typeof(ProjState)));
        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("projector-without-tags@1", typeof(ProjV1State)));
        options.ProjectionTypes.Should().ContainEquivalentOf<KeyValuePair<ProjectorId, Type>>(new("nested-projector-without-tags", typeof(ProjNestedState)));
    }
}
