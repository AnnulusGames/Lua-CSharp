using System.Globalization;
using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class ToNumberFunction : LuaFunction
{
    public override string Name => "tonumber";
    public static readonly ToNumberFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);
        int? arg1 = context.HasArgument(1)
            ? (int)context.GetArgument<double>(1)
            : null;

        if (arg1 != null && (arg1 < 2 || arg1 > 36))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'tonumber' (base out of range)");
        }

        if (arg0.Type is LuaValueType.Number)
        {
            buffer.Span[0] = arg0;
        }
        else if (arg0.TryRead<string>(out var str))
        {
            if (arg1 == null)
            {
                if (arg0.TryRead<double>(out var result))
                {
                    buffer.Span[0] = result;
                }
                else
                {
                    buffer.Span[0] = LuaValue.Nil;
                }
            }
            else if (arg1 == 10)
            {
                if (double.TryParse(str, out var result))
                {
                    buffer.Span[0] = result;
                }
                else
                {
                    buffer.Span[0] = LuaValue.Nil;
                }
            }
            else
            {
                try
                {
                    // if the base is not 10, str cannot contain a minus sign
                    var span = str.AsSpan().Trim();
                    var sign = span[0] == '-' ? -1 : 1;
                    if (sign == -1)
                    {
                        span = span[1..];
                    }
                    buffer.Span[0] = sign * StringToDouble(span, arg1.Value);
                }
                catch (FormatException)
                {
                    buffer.Span[0] = LuaValue.Nil;
                }
            }
        }
        else
        {
            buffer.Span[0] = LuaValue.Nil;
        }

        return new(1);
    }

    static double StringToDouble(ReadOnlySpan<char> text, int toBase)
    {
        var value = 0.0;
        for (int i = 0; i < text.Length; i++)
        {
            var v = text[i] switch
            {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                'a' or 'A' => 10,
                'b' or 'B' => 11,
                'c' or 'C' => 12,
                'd' or 'D' => 13,
                'e' or 'E' => 14,
                'f' or 'F' => 15,
                'g' or 'G' => 16,
                'h' or 'H' => 17,
                'i' or 'I' => 18,
                'j' or 'J' => 19,
                'k' or 'K' => 20,
                'l' or 'L' => 21,
                'm' or 'M' => 22,
                'n' or 'N' => 23,
                'o' or 'O' => 24,
                'p' or 'P' => 25,
                'q' or 'Q' => 26,
                'r' or 'R' => 27,
                's' or 'S' => 28,
                't' or 'T' => 29,
                'u' or 'U' => 30,
                'v' or 'V' => 31,
                'w' or 'W' => 32,
                'x' or 'X' => 33,
                'y' or 'Y' => 34,
                'z' or 'Z' => 35,
                _ => 0,
            };

            if (v >= toBase)
            {
                throw new FormatException();
            }

            value += v * Math.Pow(toBase, text.Length - i - 1);
        }

        return value;
    }
}