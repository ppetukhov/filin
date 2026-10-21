using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Provides a high-performance, stack-only enumerator over a DecideResult.
/// </summary>
public ref struct DecideResultEnumerator
{
    readonly int _length;
    int _index;
    readonly ReadOnlySpan<DcbEvent> _span;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DecideResultEnumerator(DcbEvent value)
    {
        Current = value;
        _length = 1;
        _index = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DecideResultEnumerator(ReadOnlySpan<DcbEvent> values)
    {
        _span = values;
        _length = values.Length;
        _index = -1;
    }

    /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var nextIndex = _index + 1;

        if (nextIndex < _length)
        {
            _index = nextIndex;
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public readonly DcbEvent Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_index < 0 || _index >= _length)
            {
                ThrowInvalidOperationException();
            }

            return field is not null ? field : _span[_index];
        }
    }

    /// <summary>
    /// Non-inlined helper method containing the exception throwing logic.
    /// This keeps the main 'get' property hot-path small and eligible for aggressive CPU inlining.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    static void ThrowInvalidOperationException() => throw new InvalidOperationException("DecideResult.DcbQueryEnumerator.Current value has not been set.");
}
