using System.Runtime.CompilerServices;

namespace Lua.Standard.Text;

internal static class StringHelper
{
    public static int UnicodeToAscii(int i)
    {
        if (i >= 0 && i <= 255) return i;
        throw new ArgumentOutOfRangeException(nameof(i));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> Slice(string s, int i, int j)
    {
        if (i < 0) i = s.Length + i + 1;
        if (j < 0) i = s.Length + i + 1;

        if (i < 1) i = 1;
        if (j > s.Length) j = s.Length;

        return i > j ? "" : s.AsSpan().Slice(i - 1, j - 1);
    }
}