using System.Runtime.CompilerServices;
using System.Text;

namespace Lua.Standard.OperatingSystem;

public sealed class DateFunction : LuaFunction
{
    public override string Name => "date";
    public static readonly DateFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var format = context.HasArgument(0)
            ? context.GetArgument<string>(0).AsSpan()
            : "%c".AsSpan();

        DateTime now;
        if (context.HasArgument(1))
        {
            var time = context.GetArgument<double>(1);
            now = DateTimeHelper.FromUnixTime(time);
        }
        else
        {
            now = DateTime.UtcNow;
        }

        var isDst = false;
        if (format[0] == '!')
        {
            format = format[1..];
        }
        else
        {
            now = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Local);
            isDst = now.IsDaylightSavingTime();
        }

        if (format == "*t")
        {
            var table = new LuaTable();
            
            table["year"] = now.Year;
            table["month"] = now.Month;
            table["day"] = now.Day;
            table["hour"] = now.Hour;
            table["min"] = now.Minute;
            table["sec"] = now.Second;
            table["wday"] = ((int)now.DayOfWeek) + 1;
            table["yday"] = now.DayOfYear;
            table["isdst"] = isDst;

            buffer.Span[0] = table;
        }
        else
        {
            buffer.Span[0] = StrFTime(context.State, format, now);
        }

        return new(1);
    }

    static string StrFTime(LuaState state, ReadOnlySpan<char> format, DateTime d)
    {
        // reference: http://www.cplusplus.com/reference/ctime/strftime/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string? STANDARD_PATTERNS(char c)
        {
            return c switch
            {
                'a' => "ddd",
                'A' => "dddd",
                'b' => "MMM",
                'B' => "MMMM",
                'c' => "f",
                'd' => "dd",
                'D' => "MM/dd/yy",
                'F' => "yyyy-MM-dd",
                'g' => "yy",
                'G' => "yyyy",
                'h' => "MMM",
                'H' => "HH",
                'I' => "hh",
                'm' => "MM",
                'M' => "mm",
                'p' => "tt",
                'r' => "h:mm:ss tt",
                'R' => "HH:mm",
                'S' => "ss",
                'T' => "HH:mm:ss",
                'y' => "yy",
                'Y' => "yyyy",
                'x' => "d",
                'X' => "T",
                'z' => "zzz",
                'Z' => "zzz",
                _ => null,
            };
        }

        var builder = new ValueStringBuilder();

        bool isEscapeSequence = false;

        for (int i = 0; i < format.Length; i++)
        {
            char c = format[i];

            if (c == '%')
            {
                if (isEscapeSequence)
                {
                    builder.Append('%');
                    isEscapeSequence = false;
                }

                continue;
            }

            if (!isEscapeSequence)
            {
                builder.Append(c);
                continue;
            }

            if (c == 'O' || c == 'E')
            {
                continue; // no modifiers
            }

            isEscapeSequence = false;

            var pattern = STANDARD_PATTERNS(c);
            if (pattern != null)
            {
                builder.Append(d.ToString(pattern));
            }
            else if (c == 'e')
            {
                var s = d.ToString("%d");
                builder.Append(s.Length < 2 ? $" {s}" : s);
            }
            else if (c == 'n')
            {
                builder.Append('\n');
            }
            else if (c == 't')
            {
                builder.Append('\t');
            }
            else if (c == 'C')
            {
                // TODO: reduce allocation
                builder.Append((d.Year / 100).ToString());
            }
            else if (c == 'j')
            {
                builder.Append(d.DayOfYear.ToString("000"));
            }
            else if (c == 'u')
            {
                int weekDay = (int)d.DayOfWeek;
                if (weekDay == 0) weekDay = 7;
                builder.Append(weekDay.ToString());
            }
            else if (c == 'w')
            {
                int weekDay = (int)d.DayOfWeek;
                builder.Append(weekDay.ToString());
            }
            else if (c == 'U')
            {
                // Week number with the first Sunday as the first day of week one (00-53)
                builder.Append("??");
            }
            else if (c == 'V')
            {
                // ISO 8601 week number (00-53)
                builder.Append("??");
            }
            else if (c == 'W')
            {
                // Week number with the first Monday as the first day of week one (00-53)
                builder.Append("??");
            }
            else
            {
                throw new LuaRuntimeException(state.GetTraceback(), $"bad argument #1 to 'date' (invalid conversion specifier '{format.ToString()}')");
            }
        }

        return builder.ToString();
    }
}