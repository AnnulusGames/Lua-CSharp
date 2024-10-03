using System.Runtime.CompilerServices;

namespace Lua.Standard.Bitwise;

internal static class Bit32Helper
{
    static readonly uint[] Masks = [
        0x1, 0x3, 0x7, 0xF, 0x1F, 0x3F, 0x7F, 0xFF,
        0x1FF, 0x3FF, 0x7FF, 0xFFF,
        0x1FFF, 0x3FFF, 0x7FFF, 0xFFFF,
        0x1FFFF, 0x3FFFF, 0x7FFFF, 0xFFFFF,
        0x1FFFFF, 0x3FFFFF, 0x7FFFFF, 0xFFFFFF,
        0x1FFFFFF, 0x3FFFFFF, 0x7FFFFFF, 0xFFFFFFF,
        0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF, 0xFFFFFFFF,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUInt32(double d)
    {
        var x = (int)Math.IEEERemainder(d, Math.Pow(2.0, 32.0));
        return (uint)x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(double d)
    {
        d = Math.IEEERemainder(d, Math.Pow(2.0, 32.0));
        return (int)d;
    }

    public static uint GetNBitMask(int bits)
    {
        if (bits <= 0) return 0;
        if (bits >= 32) return Masks[31];
        return Masks[bits - 1];
    }

    public static void ValidateFieldAndWidth(LuaState state, LuaFunction function, int argumentId, int pos, int width)
    {
        if (pos > 31 || (pos + width) > 31)
            throw new LuaRuntimeException(state.GetTraceback(), "trying to access non-existent bits");

        if (pos < 0)
            throw new LuaRuntimeException(state.GetTraceback(), $"bad argument #{argumentId} to '{function.Name}' (field cannot be negative)");

        if (width <= 0)
            throw new LuaRuntimeException(state.GetTraceback(), "bad argument #{argumentId} to '{function.Name}' (width must be positive)");
    }
}