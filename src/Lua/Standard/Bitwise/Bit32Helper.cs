using System.Runtime.CompilerServices;

namespace Lua.Standard.Bitwise;

internal static class Bit32Helper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUInt32(double d)
    {
        d = Math.IEEERemainder(d, Math.Pow(2.0, 32.0));
        return (uint)d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(double d)
    {
        d = Math.IEEERemainder(d, Math.Pow(2.0, 32.0));
        return (int)d;
    }
}