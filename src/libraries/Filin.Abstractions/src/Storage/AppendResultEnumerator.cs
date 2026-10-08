using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin.Storage;

/// <summary>
/// Provides a stack-only enumerator for iterating through a sequence of <see cref="DcbEventSequenced"/> instances.
/// </summary>
public ref struct AppendResultEnumerator
{
    readonly DcbEventSequenced _single;
    readonly IReadOnlyList<DcbEventSequenced>? _values;
    int _index = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AppendResultEnumerator(EventPosition pos, DateTimeOffset ts, DcbEvent value) => _single = new(pos, ts, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AppendResultEnumerator(IReadOnlyList<DcbEventSequenced> values) => _values = values;

    /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var nextIndex = _index + 1;

        if (nextIndex < Count())
        {
            _index = nextIndex;
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IEnumerator{DcbEventSequenced}.Current"/>
    public readonly DcbEventSequenced Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_index < 0 || _index >= Count())
            {
                ThrowInvalidOperationException();
            }

            return _values is not null ? _values[_index] : _single;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    static void ThrowInvalidOperationException() => throw new InvalidOperationException("DcbQueryEnumerator.Current value has not been set.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly int Count() => _single.Event is not null ? 1 : _values is not null ? _values.Count : 0;
}
