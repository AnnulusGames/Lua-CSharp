namespace Lua.Standard.Text;

internal static class StringHelper
{
    public static int UnicodeToAscii(int i)
    {
        if (i >= 0 && i <= 255) return i;
        throw new ArgumentOutOfRangeException(nameof(i));
    }
}