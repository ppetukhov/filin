using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Provides a high-performance, stack-only enumerator over a collection.
/// </summary>
public ref struct TagsEnumerator
{
    int _index;
    readonly ReadOnlySpan<Tag> _span;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TagsEnumerator(ReadOnlySpan<Tag> values)
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
    public readonly Tag Current
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
    static void ThrowInvalidOperationException() => throw new InvalidOperationException("TagsEnumerator.Current value has not been set.");
}

