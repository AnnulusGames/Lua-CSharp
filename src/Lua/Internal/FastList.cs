using System.Runtime.CompilerServices;

namespace Lua.Internal;

public class FastList<T>
{
    FastListCore<T> core;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return core.Length;
        }
    }

    public ref T this[int index]
    {
        get
        {
            return ref core[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T value)
    {
        core.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAtSwapback(int index)
    {
        core.RemoveAtSwapback(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return core.AsSpan();
    }
}