using Filin.Abstractions.Tests.Configuration;

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

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProjectorWithoutQueryTagsV1_GetProjectionId_Return_ProjectionId_Version1()
    {
        var result = ProjectorWithoutQueryTagsV1.GetProjectionId([]);

        var defaultKey = (IdSpan)default;
        var expected = new ProjectionId(new ProjectorId("projector-without-tags@1"), defaultKey);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NestedProjectorWithoutQueryTags_GetProjectionId_ProjectionIdWithDefaultKey()
    {
        var result = TestDomainProjectors.V0.NestedProjectorWithoutQueryTags.GetProjectionId([]);

        var defaultKey = (IdSpan)default;
        var expected = new ProjectionId(new ProjectorId("nested-projector-without-tags"), defaultKey);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProjectorWithoutQueryTags_GetQuery_Return_QueryWithOneItem()
    {
        var result = ProjectorWithoutQueryTags.GetQuery([]);

        var expected = new DcbQueryItem(["StudentSubscribed", "CourseCreated", "CourseRenamed", "CourseScheduled", "CourseCapacityChanged", "CourseStarted", "CourseFinished"], []);

        Assert.Equal(1, result.Count);
        Assert.Equal(expected, result[0]);
    }

    [Fact]
    public void ProjectorWithoutQueryTagsV1_GetQuery_Return_QueryWithOneItem()
    {
        var result = ProjectorWithoutQueryTagsV1.GetQuery([]);

        var expected = new DcbQueryItem(["StudentSubscribed", "CourseCreated", "CourseRenamed", "CourseScheduled", "CourseCapacityChanged", "CourseStarted", "CourseFinished"], []);

        Assert.Equal(1, result.Count);
        Assert.Equal(expected, result[0]);
    }

    [Fact]
    public void NestedProjectorWithoutQueryTags_GetQuery_Return_QueryWithOneItem()
    {
        var result = TestDomainProjectors.V0.NestedProjectorWithoutQueryTags.GetQuery([]);

        var expected = new DcbQueryItem(["StudentSubscribed", "CourseCreated", "CourseRenamed", "CourseScheduled", "CourseCapacityChanged", "CourseStarted", "CourseFinished"], []);

        Assert.Equal(1, result.Count);
        Assert.Equal(expected, result[0]);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectorTypes_ProjectorWithoutQueryTags()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsProjectorsTypeManifestProvider();

        provider.Configure(options);

        Assert.Contains(new KeyValuePair<Type, Type>(typeof(ProjState), typeof(ProjectorWithoutQueryTags)), options.ProjectorTypes);
        Assert.Contains(new KeyValuePair<Type, Type>(typeof(ProjV1State), typeof(ProjectorWithoutQueryTagsV1)), options.ProjectorTypes);
        Assert.Contains(new KeyValuePair<Type, Type>(typeof(ProjNestedState), typeof(TestDomainProjectors.V0.NestedProjectorWithoutQueryTags)), options.ProjectorTypes);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectionTypes_ProjectorWithoutQueryTags()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsProjectorsTypeManifestProvider();

        provider.Configure(options);

        Assert.Contains(new KeyValuePair<ProjectorId, Type>(new ProjectorId("projector-without-tags"), typeof(ProjState)), options.ProjectionTypes);
        Assert.Contains(new KeyValuePair<ProjectorId, Type>(new ProjectorId("projector-without-tags@1"), typeof(ProjV1State)), options.ProjectionTypes);
        Assert.Contains(new KeyValuePair<ProjectorId, Type>(new ProjectorId("nested-projector-without-tags"), typeof(ProjNestedState)), options.ProjectionTypes);
    }
}
