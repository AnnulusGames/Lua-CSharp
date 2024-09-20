using System.Runtime.CompilerServices;

namespace Lua;

internal static class MathEx
{
    const ulong PositiveInfinityBits = 0x7FF0_0000_0000_0000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInteger(double d)
    {
#if NET8_0_OR_GREATER
        return double.IsInteger(d);
#else
        return IsFinite(d) && (d == Math.Truncate(d));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsFinite(double d)
    {
#if NET6_0_OR_GREATER
        ulong bits = BitConverter.DoubleToUInt64Bits(d);
#else
        ulong bits = BitCast<double, ulong>(d);
#endif

        return (~bits & PositiveInfinityBits) != 0;
    }

#if !NET6_0_OR_GREATER
    unsafe static TTo BitCast<TFrom, TTo>(TFrom source)
    {
        return Unsafe.ReadUnaligned<TTo>(ref Unsafe.As<TFrom, byte>(ref source));
    }
#endif

    public const int ArrayMexLength = 0x7FFFFFC7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NewArrayCapacity(int size)
    {
        var newSize = unchecked(size * 2);
        if ((uint)newSize > ArrayMexLength)
        {
            newSize = ArrayMexLength;
        }

        return newSize;
    }
}