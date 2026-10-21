using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Provides a high-performance, stack-only enumerator over a DcbQuery.
/// </summary>
public ref struct DcbQueryEnumerator
{
    int _index;
    readonly ReadOnlySpan<DcbQueryItem> _span;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DcbQueryEnumerator(ReadOnlySpan<DcbQueryItem> values)
    {
        _span = values;
        _index = -1;
    }

    /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var nextIndex = _index + 1;
        if (nextIndex < _span.Length)
        {
            _index = nextIndex;
            return true;
        }

        _index = nextIndex;
        return false;
    }

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public readonly DcbQueryItem Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_index < 0 || _index >= _span.Length)
            {
                ThrowInvalidOperationException();
            }
            return _span[_index];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    static void ThrowInvalidOperationException() =>
        throw new InvalidOperationException("DcbQueryEnumerator.Current value has not been set.");
}

