namespace Filin.Abstractions.Tests;

public sealed class DecideResultTests
{
    [Fact]
    public void SubmissionEntry_Constructor_One_DcbEvent()
    {
        var evt = new DcbEvent { EventType = new EventType("SomeEventType"), Data = new object() };

        var result = new DecideResult(evt);

        result.Count.Should().Be(1);

        result[0].Should().BeSameAs(evt);
    }

    [Fact]
    public void SubmissionEntry_Constructor_Some_DcbEvents()
    {
        var evt1 = new DcbEvent { EventType = new EventType("SomeEventType1"), Data = new object() };
        var evt2 = new DcbEvent { EventType = new EventType("SomeEventType2"), Data = new object() };
        var evt3 = new DcbEvent { EventType = new EventType("SomeEventType3"), Data = new object() };

        var result = new DecideResult(evt1, evt2, evt3);

        result.Count.Should().Be(3);
        result[0].Should().BeSameAs(evt1);
        result[1].Should().BeSameAs(evt2);
        result[2].Should().BeSameAs(evt3);
    }

    [Fact]
    public void SubmissionEntry_Constructor_Throw_ArgumentNullExecption()
    {
        Action action = () => new DecideResult(null!);

        action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("@event");
    }
}
