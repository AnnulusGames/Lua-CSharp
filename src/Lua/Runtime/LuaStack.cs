using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        array[top] = value;
        top++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue Pop()
    {
        if (top == 0) ThrowEmptyStack();
        top--;
        var item = array[top];
        array[top] = default;
        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PopUntil(int newSize)
    {
        if (newSize >= top) return;
        array.AsSpan(newSize, top).Clear();
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
        // if (index < 0 || index >= array.Length) throw new IndexOutOfRangeException();

#if NET6_0_OR_GREATER
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
        ref var reference = ref MemoryMarshal.GetReference(array.AsSpan());
        return ref Unsafe.Add(ref reference, index);
#endif
    }

    static void ThrowEmptyStack()
    {
        throw new InvalidOperationException("Empty stack");
    }
}