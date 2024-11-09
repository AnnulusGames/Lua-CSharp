using System.Runtime.CompilerServices;
using Lua.Internal;

namespace Lua.Runtime;

public class LuaStack(int initialSize = 256)
{
    LuaValue[] array = new LuaValue[initialSize];
    int top;

    public int Count => top;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int newSize)
    {
        var size = array.Length;
        if (size >= newSize) return;

        while (size < newSize)
        {
            size *= 2;
        }

        Array.Resize(ref array, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NotifyTop(int top)
    {
        if (this.top < top) this.top = top;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(LuaValue value)
    {
        EnsureCapacity(top + 1);
        UnsafeGet(top) = value;
        top++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushRange(ReadOnlySpan<LuaValue> values)
    {
        EnsureCapacity(top + values.Length);
        values.CopyTo(array.AsSpan()[top..]);
        top += values.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue Pop()
    {
        if (top == 0) ThrowEmptyStack();
        top--;
        var item = UnsafeGet(top);
        UnsafeGet(top) = default;
        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PopUntil(int newSize)
    {
        if (newSize >= top) return;
        array.AsSpan(newSize,top-newSize).Clear();
        top = newSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        array.AsSpan().Clear();
        top = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<LuaValue> AsSpan()
    {
        return new Span<LuaValue>(array, 0, top);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<LuaValue> GetBuffer()
    {
        return array.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<LuaValue> GetBufferMemory()
    {
        return array.AsMemory();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LuaValue UnsafeGet(int index)
    {
        return ref MemoryMarshalEx.UnsafeElementAt(array, index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref LuaValue Get(int index)
    {
        return ref array[index];
    }

    static void ThrowEmptyStack()
    {
        throw new InvalidOperationException("Empty stack");
    }
}