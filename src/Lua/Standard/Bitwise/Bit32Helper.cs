using System.Runtime.CompilerServices;

namespace Lua.Standard.Bitwise;

internal static class Bit32Helper
{
    static readonly double Bit32 = 4294967296;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUInt32(double d)
    {
        return (uint)ToInt32(d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(double d)
    {
        return (int)(long)Math.IEEERemainder(d, Bit32);
    }

    public static void ValidateFieldAndWidth(LuaState state, LuaFunction function, int argumentId, int field, int width)
    {
        if (field > 31 || (field + width) > 32)
            throw new LuaRuntimeException(state.GetTraceback(), "trying to access non-existent bits");

        if (field < 0)
            throw new LuaRuntimeException(state.GetTraceback(), $"bad argument #{argumentId} to '{function.Name}' (field cannot be negative)");

        if (width <= 0)
            throw new LuaRuntimeException(state.GetTraceback(), "bad argument #{argumentId} to '{function.Name}' (width must be positive)");
    }
}