using Lua.Internal;

namespace Lua.Standard.Basic;

public sealed class ToNumberFunction : LuaFunction
{
    public override string Name => "tonumber";
    public static readonly ToNumberFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var e = context.GetArgument(0);
        int? toBase = context.HasArgument(1)
            ? (int)context.GetArgument<double>(1)
            : null;

        if (toBase != null && (toBase < 2 || toBase > 36))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'tonumber' (base out of range)");
        }

        double? value = null;
        if (e.Type is LuaValueType.Number)
        {
            value = e.Read<double>();
        }
        else if (e.TryRead<string>(out var str))
        {
            if (toBase == null)
            {
                if (e.TryRead<double>(out var result))
                {
                    value = result;
                }
            }
            else if (toBase == 10)
            {
                if (double.TryParse(str, out var result))
                {
                    value = result;
                }
            }
            else
            {
                try
                {
                    // if the base is not 10, str cannot contain a minus sign
                    var span = str.AsSpan().Trim();
                    if (span.Length == 0) goto END;

                    var first = span[0];
                    var sign = first == '-' ? -1 : 1;
                    if (first is '+' or '-')
                    {
                        span = span[1..];
                    }
                    if (span.Length == 0) goto END;

                    if (toBase == 16 && span.Length > 2 && span[0] is '0' && span[1] is 'x' or 'X')
                    {
                        value = sign * HexConverter.ToDouble(span);
                    }
                    else
                    {
                        value = sign * StringToDouble(span, toBase.Value);
                    }
                }
                catch (FormatException)
                {
                    goto END;
                }
            }
        }
        else
        {
            goto END;
        }

    END:
        if (value != null && double.IsNaN(value.Value))
        {
            value = null;
        }

        buffer.Span[0] = value == null ? LuaValue.Nil : value.Value;
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