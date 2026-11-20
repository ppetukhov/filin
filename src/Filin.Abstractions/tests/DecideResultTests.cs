namespace Filin.Abstractions.Tests;

public sealed class DecideResultTests
{
    [Fact]
    public void SubmissionEntry_Constructor_One_DcbEvent()
    {
        var evt = new DcbEvent { EventType = new EventType("SomeEventType"), Payload = new object() };

        var result = new DecideResult(evt);

        Assert.Equal(1, result.Count);
        Assert.Same(evt, result[0]);
    }

    [Fact]
    public void SubmissionEntry_Constructor_Some_DcbEvents()
    {
        var evt1 = new DcbEvent { EventType = new EventType("SomeEventType1"), Payload = new object() };
        var evt2 = new DcbEvent { EventType = new EventType("SomeEventType2"), Payload = new object() };
        var evt3 = new DcbEvent { EventType = new EventType("SomeEventType3"), Payload = new object() };

        var result = new DecideResult(evt1, evt2, evt3);

        Assert.Equal(3, result.Count);
        Assert.Same(evt1, result[0]);
        Assert.Same(evt2, result[1]);
        Assert.Same(evt3, result[2]);
    }

    [Fact]
    public void SubmissionEntry_Constructor_Throw_ArgumentNullExecption()
    {
#pragma warning disable CA1806 // Do not ignore method results
#pragma warning disable IDE0039 // Use local function
        Action action = static () => new DecideResult(null!);
#pragma warning restore IDE0039 // Use local function
#pragma warning restore CA1806 // Do not ignore method results

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("@event", exception.ParamName);
    }
}
