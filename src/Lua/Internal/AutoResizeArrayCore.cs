using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lua.Internal;

internal struct AutoResizeArrayCore<T>
{
    T[]? array;
    int size;

    public int Size => size;
    public int Capacity => array == null ? 0 : array.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        this[size] = item;
    }

    public ref T this[int index]
    {
        get
        {
            EnsureCapacity(index);
            size = Math.Max(size, index + 1);

#if NET6_0_OR_GREATER
            ref var reference = ref MemoryMarshal.GetArrayDataReference(array!);
#else
            ref var reference = ref MemoryMarshal.GetReference(array.AsSpan());
#endif

            return ref Unsafe.Add(ref reference, index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return array.AsSpan(0, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] GetInternalArray()
    {
        return array ?? [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        array.AsSpan().Clear();
    }

    public void EnsureCapacity(int newCapacity, bool overrideSize = false)
    {
        var capacity = 64;
        while (capacity <= newCapacity)
        {
            capacity = MathEx.NewArrayCapacity(capacity);
        }

        if (array == null)
        {
            array = new T[capacity];
        }
        else
        {
            Array.Resize(ref array, capacity);
        }

        if (overrideSize)
        {
            size = newCapacity;
        }
    }

    public void Shrink(int newSize)
    {
        if (array != null && array.Length > newSize)
        {
            array.AsSpan(newSize).Clear();
        }

        size = newSize;
    }
}