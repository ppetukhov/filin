using Filin.Abstractions.Tests.Configuration;

namespace Filin.Abstractions.ProjectorTests;

[ProjectorId("projector-with-query-tag-parameters")]
[QueryEventTypes("CourseCreated")]
[QueryTagParameters(0, "course", "teacher")]
[QueryEventTypes(1, "CourseRenamed", "CourseCapacityChanged", "CourseScheduled")]
[QueryTagParameters(1, "course")]
[QueryEventTypes(2, "CourseStarted", "StudentSubscribed", "CourseFinished")]
[QueryTagParameters(2, "course", "student")]
[QueryEventTypes(10, "CoursesPaused", "CoursesResumed")]
sealed partial class ProjectorWithQueryTagParameters : IProjector<ProjParamsState>
{
    public static ProjParamsState Project(ProjParamsState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static ProjParamsState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

[ProjectorId("projector-with-query-tag-parameters", 1)]
[QueryEventTypes("CourseCreated")]
[QueryTagParameters(0, "course", "teacher")]
[QueryEventTypes(1, "CourseRenamed", "CourseCapacityChanged", "CourseScheduled")]
[QueryTagParameters(1, "course")]
[QueryEventTypes(2, "CourseStarted", "StudentSubscribed", "CourseFinished")]
[QueryTagParameters(2, "course", "student")]
[QueryEventTypes(10, "CoursesPaused", "CoursesResumed")]
sealed partial class ProjectorWithQueryTagParametersV1 : IProjector<ProjParamsV1State>
{
    public static ProjParamsV1State Project(ProjParamsV1State state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
    public static ProjParamsV1State Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
}

sealed partial class TestDomainProjectors
{
    internal static partial class V0
    {
        [ProjectorId("nested-projector-with-query-tag-parameters")]
        [QueryEventTypes("CourseCreated")]
        [QueryTagParameters(0, "course", "teacher")]
        [QueryEventTypes(1, "CourseRenamed", "CourseCapacityChanged", "CourseScheduled")]
        [QueryTagParameters(1, "course")]
        [QueryEventTypes(2, "CourseStarted", "StudentSubscribed", "CourseFinished")]
        [QueryTagParameters(2, "course", "student")]
        [QueryEventTypes(10, "CoursesPaused", "CoursesResumed")]
        internal sealed partial record NestedProjectorWithQueryTagParameters : IProjector<ProjNestedParamsState>
        {
            public static ProjNestedParamsState Project(ProjNestedParamsState state, DcbEventSequenced domainEvent) => throw new NotImplementedException();
            public static ProjNestedParamsState Project(DcbEventSequenced domainEvent) => throw new NotImplementedException();
        }
    }
}

sealed record ProjParamsState { };
sealed record ProjParamsV1State { };
sealed record ProjNestedParamsState { };

public sealed class QueryTagParametersAttributeTests
{
    [Fact]
    public void ProjectorWithQueryTagParameters_GetProjectionId_Return_ProjectionId_WithKeyOrderedByTagParameter()
    {
        Tag tagCourse = "course:5555";
        Tag tagStudent1 = "student:john";
        Tag tagStudent2 = "student:smiths";
        Tag tagTeacher = "teacher:584456";

        SortedSet<Tag> exactTags = [tagCourse, tagTeacher, tagStudent1, tagStudent2];
        Tags tags = [.. exactTags, "other-entity:846123", "owner:4123"];

        var result = ProjectorWithQueryTagParameters.GetProjectionId(tags);

        var orderedKey = string.Join('&', exactTags.Order());
        var expected = new ProjectionId(new ProjectorId("projector-with-query-tag-parameters"), orderedKey);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProjectorWithQueryTagParametersV1_GetProjectionId_Return_ProjectionId_Version1_WithKeyOrderedByTagParameter()
    {
        Tag tagCourse = "course:5555";
        Tag tagStudent1 = "student:john";
        Tag tagStudent2 = "student:smiths";
        Tag tagTeacher = "teacher:584456";

        SortedSet<Tag> exactTags = [tagCourse, tagTeacher, tagStudent1, tagStudent2];
        Tags tags = [.. exactTags, "other-entity:846123", "owner:4123"];

        var result = ProjectorWithQueryTagParametersV1.GetProjectionId(tags);

        var orderedKey = string.Join('&', exactTags.Order());
        var expected = new ProjectionId(new ProjectorId("projector-with-query-tag-parameters@1"), orderedKey);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NestedProjectorWithQueryTagParameters_GetProjectionId_ProjectionId_WithKeyOrderedByTagParameter()
    {
        Tag tagCourse = "course:5555";
        Tag tagStudent1 = "student:john";
        Tag tagStudent2 = "student:smiths";
        Tag tagTeacher = "teacher:584456";

        SortedSet<Tag> exactTags = [tagCourse, tagTeacher, tagStudent1, tagStudent2];
        Tags tags = [.. exactTags, "other-entity:846123", "owner:4123"];

        var result = TestDomainProjectors.V0.NestedProjectorWithQueryTagParameters.GetProjectionId(tags);

        var orderedKey = string.Join('&', exactTags.Order());
        var expected = new ProjectionId(new ProjectorId("nested-projector-with-query-tag-parameters"), orderedKey);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProjectorWithQueryTagParameters_GetQuery_Return_QueryWithFourItems()
    {
        Tag tagCourse = "course:5555";
        Tag tagStudent1 = "student:john";
        Tag tagStudent2 = "student:smiths";
        Tag tagTeacher = "teacher:584456";

        SortedSet<Tag> exactTags = [tagCourse, tagTeacher, tagStudent1, tagStudent2];
        Tags tags = [.. exactTags, "other-entity:846123", "owner:4123"];

        var result = ProjectorWithQueryTagParameters.GetQuery(tags);

        var expected1 = new DcbQueryItem(["CourseCreated"], [tagCourse, tagTeacher]);
        var expected2 = new DcbQueryItem(["CourseRenamed", "CourseCapacityChanged", "CourseScheduled"], [tagCourse]);
        var expected3 = new DcbQueryItem(["CoursesPaused", "CoursesResumed"], []);
        var expected4 = new DcbQueryItem(["CourseStarted", "StudentSubscribed", "CourseFinished"], [tagCourse, tagStudent1, tagStudent2]);

        Assert.Equal(4, result.Count);
        Assert.True(result.Contains(expected1));
        Assert.True(result.Contains(expected2));
        Assert.True(result.Contains(expected3));
        Assert.True(result.Contains(expected4));
    }

    [Fact]
    public void ProjectorWithQueryTagParametersV1_GetQuery_Return_QueryWithFourItems()
    {
        Tag tagCourse = "course:5555";
        Tag tagStudent1 = "student:john";
        Tag tagStudent2 = "student:smiths";
        Tag tagTeacher = "teacher:584456";

        Tags tags = [tagCourse, tagTeacher, tagStudent1, tagStudent2, "other-entity:846123", "owner:4123"];

        var result = ProjectorWithQueryTagParametersV1.GetQuery(tags);

        var expected1 = new DcbQueryItem(["CourseCreated"], [tagCourse, tagTeacher]);
        var expected2 = new DcbQueryItem(["CourseRenamed", "CourseCapacityChanged", "CourseScheduled"], [tagCourse]);
        var expected3 = new DcbQueryItem(["CoursesPaused", "CoursesResumed"], []);
        var expected4 = new DcbQueryItem(["CourseStarted", "StudentSubscribed", "CourseFinished"], [tagCourse, tagStudent1, tagStudent2]);

        Assert.Equal(4, result.Count);
        Assert.True(result.Contains(expected1));
        Assert.True(result.Contains(expected2));
        Assert.True(result.Contains(expected3));
        Assert.True(result.Contains(expected4));
    }

    [Fact]
    public void NestedProjectorWithQueryTagParameters_GetQuery_Return_QueryWithFourItems()
    {
        Tag tagCourse = "course:5555";
        Tag tagStudent1 = "student:john";
        Tag tagStudent2 = "student:smiths";
        Tag tagTeacher = "teacher:584456";

        Tags tags = [tagCourse, tagTeacher, tagStudent1, tagStudent2, "other-entity:846123", "owner:4123"];

        var result = TestDomainProjectors.V0.NestedProjectorWithQueryTagParameters.GetQuery(tags);

        var expected1 = new DcbQueryItem(["CourseCreated"], [tagCourse, tagTeacher]);
        var expected2 = new DcbQueryItem(["CourseRenamed", "CourseCapacityChanged", "CourseScheduled"], [tagCourse]);
        var expected3 = new DcbQueryItem(["CoursesPaused", "CoursesResumed"], []);
        var expected4 = new DcbQueryItem(["CourseStarted", "StudentSubscribed", "CourseFinished"], [tagCourse, tagStudent1, tagStudent2]);

        Assert.Equal(4, result.Count);
        Assert.True(result.Contains(expected1));
        Assert.True(result.Contains(expected2));
        Assert.True(result.Contains(expected3));
        Assert.True(result.Contains(expected4));
    }

    [Fact]
    public void ProjectorWithQueryTagParameters_GetProjectionId_Throw_ArgumentException_When_Notfound_Student_Tag()
    {
        Tags tags = ["course:5555", "teacher:584456", "other-entity:846123", "owner:4123"];

#pragma warning disable IDE0039 // Use local function
        Action act = () => ProjectorWithQueryTagParameters.GetProjectionId(tags);
#pragma warning restore IDE0039 // Use local function

        var ex = Assert.Throws<ArgumentException>(act);
        Assert.StartsWith("Required tag started with student:", ex.Message, StringComparison.Ordinal);
        Assert.Equal("tags", ex.ParamName);
        Assert.True(ex.Data.Contains("TagParameter"));
        Assert.Equal("student:", ex.Data["TagParameter"]);
        Assert.True(ex.Data.Contains("Tags"));
        Assert.Equal(tags.ToString(), ex.Data["Tags"]);
    }

    [Fact]
    public void ProjectorWithQueryTagParametersV1_GetProjectionId_Throw_ArgumentException_When_Notfound_Student_Tag()
    {
        Tags tags = ["course:5555", "teacher:584456", "other-entity:846123", "owner:4123"];

#pragma warning disable IDE0039 // Use local function
        Action act = () => ProjectorWithQueryTagParametersV1.GetProjectionId(tags);
#pragma warning restore IDE0039 // Use local function

        var ex = Assert.Throws<ArgumentException>(act);
        Assert.StartsWith("Required tag started with student:", ex.Message, StringComparison.Ordinal);
        Assert.Equal("tags", ex.ParamName);
        Assert.True(ex.Data.Contains("TagParameter"));
        Assert.Equal("student:", ex.Data["TagParameter"]);
        Assert.True(ex.Data.Contains("Tags"));
        Assert.Equal(tags.ToString(), ex.Data["Tags"]);
    }

    [Fact]
    public void NestedProjectorWithQueryTagParameters_GetProjectionId_Throw_ArgumentException_When_Notfound_Student_Tag()
    {
        Tags tags = ["course:5555", "teacher:584456", "other-entity:846123", "owner:4123"];

#pragma warning disable IDE0039 // Use local function
        Action act = () => TestDomainProjectors.V0.NestedProjectorWithQueryTagParameters.GetProjectionId(tags);
#pragma warning restore IDE0039 // Use local function

        var ex = Assert.Throws<ArgumentException>(act);
        Assert.StartsWith("Required tag started with student:", ex.Message, StringComparison.Ordinal);
        Assert.Equal("tags", ex.ParamName);
        Assert.True(ex.Data.Contains("TagParameter"));
        Assert.Equal("student:", ex.Data["TagParameter"]);
        Assert.True(ex.Data.Contains("Tags"));
        Assert.Equal(tags.ToString(), ex.Data["Tags"]);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectorTypes_ProjectorParameters()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsProjectorsTypeManifestProvider();

        provider.Configure(options);

        Assert.Contains(new KeyValuePair<Type, Type>(typeof(ProjParamsState), typeof(ProjectorWithQueryTagParameters)), options.ProjectorTypes);
        Assert.Contains(new KeyValuePair<Type, Type>(typeof(ProjParamsV1State), typeof(ProjectorWithQueryTagParametersV1)), options.ProjectorTypes);
        Assert.Contains(new KeyValuePair<Type, Type>(typeof(ProjNestedParamsState), typeof(TestDomainProjectors.V0.NestedProjectorWithQueryTagParameters)), options.ProjectorTypes);
    }

    [Fact]
    public void MetadataProjectors_Configure_ProjectionTypes_ProjectorParameters()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsProjectorsTypeManifestProvider();

        provider.Configure(options);

        Assert.Contains(new KeyValuePair<ProjectorId, Type>(new ProjectorId("projector-with-query-tag-parameters"), typeof(ProjParamsState)), options.ProjectionTypes);
        Assert.Contains(new KeyValuePair<ProjectorId, Type>(new ProjectorId("projector-with-query-tag-parameters@1"), typeof(ProjParamsV1State)), options.ProjectionTypes);
        Assert.Contains(new KeyValuePair<ProjectorId, Type>(new ProjectorId("nested-projector-with-query-tag-parameters"), typeof(ProjNestedParamsState)), options.ProjectionTypes);
    }
}
