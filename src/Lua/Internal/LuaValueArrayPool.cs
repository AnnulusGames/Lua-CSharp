namespace Lua.Internal;

internal static class LuaValueArrayPool
{
    static FastStackCore<LuaValue[]> poolOf1024;
    static FastStackCore<LuaValue[]> poolOf1;

    static readonly object lockObject = new();


    public static LuaValue[] Rent1024()
    {
        lock (lockObject)
        {
            if (poolOf1024.Count > 0)
            {
                return poolOf1024.Pop();
            }

            return new LuaValue[1024];
        }
    }

    public static LuaValue[] Rent1()
    {
        lock (lockObject)
        {
            if (poolOf1.Count > 0)
            {
                return poolOf1.Pop();
            }

            return new LuaValue[1];
        }
    }

    public static void Return1024(LuaValue[] array,bool clear=false)
    {
        if (array.Length != 1024)
        {
            ThrowInvalidArraySize(array.Length, 1024);
        }

        if (clear)
        {
            array.AsSpan().Clear();
        }
        lock (lockObject)
        {
            poolOf1024.Push(array);
        }
    }


    public static void Return1(LuaValue[] array)
    {
        if (array.Length != 1)
        {
            ThrowInvalidArraySize(array.Length, 1);
        }

        array[0] = LuaValue.Nil;
        lock (lockObject)
        {
            poolOf1.Push(array);
        }
    }

    static void ThrowInvalidArraySize(int size, int expectedSize)
    {
        throw new InvalidOperationException($"Invalid array size: {size}, expected: {expectedSize}");
    }
}