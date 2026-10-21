using Filin.Abstractions.Tests.Configuration;

namespace Filin.Abstractions.DeciderTests;

[Decider]
sealed class CreateCourseDecider : IDecider<CreateCourse, ActiveCourses>
{
    public static DecideResult Decide(CreateCourse command, ActiveCourses state) => throw new NotImplementedException();
    public static DecideResult Decide(CreateCourse command) => throw new NotImplementedException();
}

sealed record CreateCourse : ICommand<ActiveCourses>
{
    public Tags GetTags() => ["course:123"];
};

sealed record ActiveCourses { };

public sealed class DeciderAttributeTests
{
    [Fact]
    public void MetadataDeciders_Configure_DeciderTypes_TotalCount()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsDecidersTypeManifestProvider();

        provider.Configure(options);

        options.DeciderTypes.Should().ContainSingle();
    }

    [Fact]
    public void MetadataDeciders_Configure_DeciderTypes()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsDecidersTypeManifestProvider();

        provider.Configure(options);

        options.DeciderTypes.Should().ContainEquivalentOf<KeyValuePair<(Type, Type), Type>>(new((typeof(CreateCourse), typeof(ActiveCourses)), typeof(CreateCourseDecider)));
    }
}
