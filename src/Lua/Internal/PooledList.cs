using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lua.Internal;

internal ref struct PooledList<T>
{
    T[]? buffer;
    int tail;

    public PooledList(int sizeHint)
    {
        buffer = ArrayPool<T>.Shared.Rent(sizeHint);
    }

    public bool IsDisposed => tail == -1;
    public int Count => tail;

    public void Add(in T item)
    {
        ThrowIfDisposed();

        if (buffer == null)
        {
            buffer = ArrayPool<T>.Shared.Rent(32);
        }
        else if (buffer.Length == tail)
        {
            var newArray = ArrayPool<T>.Shared.Rent(tail * 2);
            buffer.AsSpan().CopyTo(newArray);
            ArrayPool<T>.Shared.Return(buffer);
            buffer = newArray;
        }

        buffer[tail] = item;
        tail++;
    }

    public void Clear()
    {
        ThrowIfDisposed();

        if (buffer != null)
        {
            new Span<T>(buffer, 0, tail).Clear();
        }

        tail = 0;
    }

    public void Dispose()
    {
        ThrowIfDisposed();

        if (buffer != null)
        {
            ArrayPool<T>.Shared.Return(buffer);
            buffer = null;
        }

        tail = -1;
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return AsSpan()[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsSpan()
    {
        return new ReadOnlySpan<T>(buffer, 0, tail);
    }

    void ThrowIfDisposed()
    {
        if (tail == -1) throw new ObjectDisposedException(nameof(PooledList<T>));
    }
}