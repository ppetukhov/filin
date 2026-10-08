using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Enumerates the elements of an <see cref="EventTypes"/> using a stack-allocated structure.
/// </summary>
public ref struct EventTypeEnumerator
{
    int _index;
    readonly ReadOnlySpan<EventType> _span;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeEnumerator"/> struct with the specified span of event types.
    /// </summary>
    /// <param name="values">The underlying span sequence to iterate over.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EventTypeEnumerator(ReadOnlySpan<EventType> values)
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

    /// <summary>
    /// Gets the element in the collection at the current position of the enumerator.
    /// </summary>
    /// <value>The <see cref="EventType"/> element in the collection at the current position of the enumerator.</value>
    /// <exception cref="InvalidOperationException">Thrown if the enumerator is positioned before the first element or after the last element.</exception>
    public readonly EventType Current
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
        throw new InvalidOperationException("EventTypeEnumerator.Current value has not been set.");
}

