using System.Runtime.CompilerServices;
using System.Text;

namespace Lua.Internal;

internal static class StringHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> Slice(string s, int i, int j)
    {
        if (i < 0) i = s.Length + i + 1;
        if (j < 0) j = s.Length + j + 1;

        if (i < 1) i = 1;
        if (j > s.Length) j = s.Length;

        return i > j ? "" : s.AsSpan()[(i - 1)..j];
    }

    public static string FromStringLiteral(ReadOnlySpan<char> literal)
    {
        var builder = new ValueStringBuilder(literal.Length);
        for (int i = 0; i < literal.Length; i++)
        {
            var c = literal[i];
            if (c is '\\' && i < literal.Length - 1)
            {
                i++;
                c = literal[i];

                switch (c)
                {
                    case 'a':
                        builder.Append('\a');
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'v':
                        builder.Append('\v');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    case '\"':
                        builder.Append('\"');
                        break;
                    case '\'':
                        builder.Append('\'');
                        break;
                    case '[':
                        builder.Append('[');
                        break;
                    case ']':
                        builder.Append(']');
                        break;
                }
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}