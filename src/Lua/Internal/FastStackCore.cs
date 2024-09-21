using System.Runtime.InteropServices;

namespace Lua.Internal;

[StructLayout(LayoutKind.Auto)]
public struct FastStackCore<T>
{
    const int InitialCapacity = 8;

    T?[] array;
    int tail;

    public int Size => tail;

    public readonly ReadOnlySpan<T> AsSpan()
    {
        if (array == null) return [];
        return array.AsSpan(0, tail);
    }

    public readonly T? this[int index]
    {
        get
        {
            return array[index];
        }
    }

    public void Push(in T item)
    {
        array ??= new T[InitialCapacity];

        if (tail == array.Length)
        {
            var newArray = new T[tail * 2];
            Array.Copy(array, newArray, tail);
            array = newArray;
        }

        array[tail] = item;
        tail++;
    }

    public bool TryPop(out T value)
    {
        if (tail == 0)
        {
            value = default!;
            return false;
        }

        tail--;
        value = array[tail]!;
        array[tail] = default;

        return true;
    }

    public T Pop()
    {
        if (!TryPop(out var result)) throw new InvalidOperationException("Empty stack");
        return result;
    }

    public bool TryPeek(out T value)
    {
        if (tail == 0)
        {
            value = default!;
            return false;
        }

        value = array[tail - 1]!;
        return true;
    }

    public T Peek()
    {
        if (!TryPeek(out var result)) throw new InvalidOperationException();
        return result;
    }

    public void Clear()
    {
        array.AsSpan(0, tail).Clear();
        tail = 0;
    }

    public void CopyTo(ref FastStackCore<T> destination)
    {
        if (destination.array.Length < array.Length)
        {
            Array.Resize(ref destination.array, array.Length);
        }

        array.AsSpan().CopyTo(destination.array);
        destination.tail = tail;
    }
}
