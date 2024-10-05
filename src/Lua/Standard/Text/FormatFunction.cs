
using System.Text;
using Lua.Internal;

namespace Lua.Standard.Text;

// Ignore 'p' format
 
public sealed class FormatFunction : LuaFunction
{
    public override string Name => "format";
    public static readonly FormatFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var format = context.GetArgument<string>(0);

        // TODO: pooling StringBuilder
        var builder = new StringBuilder(format.Length * 2);
        var parameterIndex = 1;

        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] == '%')
            {
                i++;

                // escape
                if (format[i] == '%')
                {
                    builder.Append('%');
                    continue;
                }
                
                var leftJustify = false;
                var zeroPadding = false;
                var alternateForm = false;
                var blank = false;
                var width = 0;
                var precision = -1;

                // Process flags
                while (true)
                {
                    var c = format[i];
                    switch (c)
                    {
                        case '-':
                            leftJustify = true;
                            break;
                        case '0':
                            zeroPadding = true;
                            break;
                        case '#':
                            alternateForm = true;
                            break;
                        case ' ':
                            blank = true;
                            break;
                        default:
                            goto PROCESS_WIDTH;
                    }

                    i++;
                }

                PROCESS_WIDTH:

                // Process width
                if (char.IsDigit(format[i]))
                {
                    var start = i;

                    do i++;
                    while (format.Length > i && char.IsDigit(format[i]));

                    width = int.Parse(format.AsSpan()[start..i]);
                }

                // Process precision
                if (format[i] == '.')
                {
                    i++;
                    var start = i;

                    do i++;
                    while (format.Length > i && char.IsDigit(format[i]));

                    precision = int.Parse(format.AsSpan()[start..i]);
                }

                // Process conversion specifier
                var specifier = format[i];
                var parameter = context.GetArgument(parameterIndex++);

                // TODO: reduce allocation
                string formattedValue = default!;
                switch (specifier)
                {
                    case 'f':
                    case 'e':
                    case 'g':
                    case 'G':
                        if (!parameter.TryRead<double>(out var f))
                        {
                            LuaRuntimeException.BadArgument(context.State.GetTraceback(), parameterIndex + 1, "format", LuaValueType.Number.ToString(), parameter.Type.ToString());
                        }

                        switch (specifier)
                        {
                            case 'f':
                                formattedValue = precision < 0
                                    ? f.ToString()
                                    : f.ToString($"F{precision}");
                                break;
                            case 'e':
                                formattedValue = precision < 0
                                    ? f.ToString()
                                    : f.ToString($"E{precision}");
                                break;
                            case 'g':
                                formattedValue = precision < 0
                                    ? f.ToString()
                                    : f.ToString($"G{precision}");
                                break;
                            case 'G':
                                formattedValue = precision < 0
                                    ? f.ToString().ToUpper()
                                    : f.ToString($"G{precision}").ToUpper();
                                break;
                        }

                        break;
                    case 's':
                        using (var strBuffer = new PooledArray<LuaValue>(1))
                        {
                            await parameter.CallToStringAsync(context, strBuffer.AsMemory(), cancellationToken);
                            formattedValue = strBuffer[0].Read<string>();
                        }

                        if (specifier is 's' && precision > 0 && precision <= formattedValue.Length)
                        {
                            formattedValue = formattedValue[..precision];
                        }
                        break;
                    case 'q':
                        switch (parameter.Type)
                        {
                            case LuaValueType.Nil:
                                formattedValue = "nil";
                                break;
                            case LuaValueType.Boolean:
                                formattedValue = parameter.Read<bool>() ? "true" : "false";
                                break;
                            case LuaValueType.String:
                                formattedValue = $"\"{StringHelper.Escape(parameter.Read<string>())}\"";
                                break;
                            case LuaValueType.Number:
                                // TODO: floating point numbers must be in hexadecimal notation
                                formattedValue = parameter.Read<double>().ToString();
                                break;
                            default:
                                using (var strBuffer = new PooledArray<LuaValue>(1))
                                {
                                    await parameter.CallToStringAsync(context, strBuffer.AsMemory(), cancellationToken);
                                    formattedValue = strBuffer[0].Read<string>();
                                }
                                break;
                        }
                        break;
                    case 'i':
                    case 'd':
                    case 'u':
                    case 'c':
                    case 'x':
                    case 'X':
                        if (!parameter.TryRead<double>(out var x))
                        {
                            LuaRuntimeException.BadArgument(context.State.GetTraceback(), parameterIndex + 1, "format", LuaValueType.Number.ToString(), parameter.Type.ToString());
                        }

                        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, parameterIndex + 1, x);

                        var integer = (long)x;

                        switch (specifier)
                        {
                            case 'i':
                            case 'd':
                            case 'u':
                                formattedValue = precision < 0 
                                    ? integer.ToString() 
                                    : integer.ToString($"D{precision}");
                                break;
                            case 'c':
                                formattedValue = ((char)integer).ToString();
                                break;
                            case 'x':
                                formattedValue = alternateForm
                                    ? $"0x{integer:x}"
                                    : $"{integer:x}";
                                break;
                            case 'X':
                                formattedValue = alternateForm
                                    ? $"0X{integer:X}"
                                    : $"{integer:X}";
                                break;
                            case 'o':
                                formattedValue = Convert.ToString(integer, 8);
                                break;
                        }
                        break;
                    default:
                        throw new LuaRuntimeException(context.State.GetTraceback(), $"invalid option '%{specifier}' to 'format'");
                }

                // Apply blank (' ') flag for positive numbers
                if (specifier is 'd' or 'i' or 'f' or 'g' or 'G')
                {
                    if (blank && !leftJustify && !zeroPadding && parameter.Read<double>() >= 0)
                    {
                        formattedValue = $" {formattedValue}";
                    }
                }

                // Apply width and padding
                if (width > formattedValue.Length)
                {
                    if (leftJustify)
                    {
                        formattedValue = formattedValue.PadRight(width);
                    }
                    else
                    {
                        formattedValue = zeroPadding ? formattedValue.PadLeft(width, '0') : formattedValue.PadLeft(width);
                    }
                }

                builder.Append(formattedValue);
            }
            else
            {
                builder.Append(format[i]);
            }
        }


        buffer.Span[0] = builder.ToString();
        return 1;
    }
}