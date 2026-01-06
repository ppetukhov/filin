using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Provides a high-performance, stack-only enumerator over a collection.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public ref struct Enumerator<T> where T : notnull
{
    readonly T? _value;
    readonly ReadOnlySpan<T> _values;
    readonly int _length;
    int _index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Enumerator(T value)
    {
        _value = value;
        _length = 1;
        _index = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Enumerator(ReadOnlySpan<T> values)
    {
        _values = values;
        _length = values.Length;
        _index = -1;
    }

    /// <summary>
    /// Advances the enumerator to the next element of the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next element;
    /// <see langword="false"/> if the enumerator has passed the end of the collection.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        int nextIndex = _index + 1;

        if (nextIndex < _length)
        {
            _index = nextIndex;
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public readonly T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_index < 0 || _index >= _length)
            {
                ThrowInvalidOperationException();
            }

            return _value is not null ? _value : _values[_index];
        }
    }

    /// <summary>
    /// Non-inlined helper method containing the exception throwing logic.
    /// This keeps the main 'get' property hot-path small and eligible for aggressive CPU inlining.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    static void ThrowInvalidOperationException() => throw new InvalidOperationException(ErrorMessages.UninitializedValue);

    /// <summary>
    /// Embedded static error helper cache to prevent dynamic runtime string interpolation allocations.
    /// </summary>
    static class ErrorMessages
    {
        public const string UninitializedValue = $"Enumerator<{nameof(T)}>.Current value has not been set.";
    }
}
