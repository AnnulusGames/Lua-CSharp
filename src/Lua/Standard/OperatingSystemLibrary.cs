using System.Diagnostics;
using Lua.Standard.Internal;

namespace Lua.Standard;

public static class OperatingSystemLibrary
{
    public static void OpenOperatingSystemLibrary(this LuaState state)
    {
        var os = new LuaTable(0, Functions.Length);
        foreach (var func in Functions)
        {
            os[func.Name] = func;
        }

        state.Environment["os"] = os;
        state.LoadedModules["os"] = os;
    }

    static readonly LuaFunction[] Functions = [
        new("clock", Clock),
        new("date", Date),
        new("difftime", DiffTime),
        new("execute", Execute),
        new("exit", Exit),
        new("getenv", GetEnv),
        new("remove", Remove),
        new("rename", Rename),
        new("setlocale", SetLocale),
        new("time", Time),
        new("tmpname", TmpName),
    ];

    public static ValueTask<int> Clock(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = DateTimeHelper.GetUnixTime(DateTime.UtcNow, Process.GetCurrentProcess().StartTime);
        return new(1);
    }

    public static ValueTask<int> Date(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
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
            buffer.Span[0] = DateTimeHelper.StrFTime(context.State, format, now);
        }

        return new(1);
    }

    public static ValueTask<int> DiffTime(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var t2 = context.GetArgument<double>(0);
        var t1 = context.GetArgument<double>(1);
        buffer.Span[0] = t2 - t1;
        return new(1);
    }

    public static ValueTask<int> Execute(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        // os.execute(command) is not supported

        if (context.HasArgument(0))
        {
            throw new NotSupportedException("os.execute(command) is not supported");
        }
        else
        {
            buffer.Span[0] = false;
            return new(1);
        }
    }

    public static ValueTask<int> Exit(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        // Ignore 'close' parameter

        if (context.HasArgument(0))
        {
            var code = context.Arguments[0];

            if (code.TryRead<bool>(out var b))
            {
                Environment.Exit(b ? 0 : 1);
            }
            else if (code.TryRead<double>(out var d))
            {
                if (!MathEx.IsInteger(d))
                {
                    throw new LuaRuntimeException(context.State.GetTraceback(), $"bad argument #1 to 'exit' (number has no integer representation)");
                }

                Environment.Exit((int)d);
            }
            else
            {
                LuaRuntimeException.BadArgument(context.State.GetTraceback(), 1, "exit", LuaValueType.Nil.ToString(), code.Type.ToString());
            }
        }
        else
        {
            Environment.Exit(0);
        }

        return new(0);
    }

    public static ValueTask<int> GetEnv(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var variable = context.GetArgument<string>(0);
        buffer.Span[0] = Environment.GetEnvironmentVariable(variable) ?? LuaValue.Nil;
        return new(1);
    }

    public static ValueTask<int> Remove(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var fileName = context.GetArgument<string>(0);
        try
        {
            File.Delete(fileName);
            buffer.Span[0] = true;
            return new(1);
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }
    }

    public static ValueTask<int> Rename(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var oldName = context.GetArgument<string>(0);
        var newName = context.GetArgument<string>(1);
        try
        {
            File.Move(oldName, newName);
            buffer.Span[0] = true;
            return new(1);
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }
    }

    public static ValueTask<int> SetLocale(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        // os.setlocale is not supported (always return nil)

        buffer.Span[0] = LuaValue.Nil;
        return new(1);
    }

    public static ValueTask<int> Time(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (context.HasArgument(0))
        {
            var table = context.GetArgument<LuaTable>(0);
            var date = DateTimeHelper.ParseTimeTable(context.State, table);
            buffer.Span[0] = DateTimeHelper.GetUnixTime(date);
            return new(1);
        }
        else
        {
            buffer.Span[0] = DateTimeHelper.GetUnixTime(DateTime.UtcNow);
            return new(1);
        }
    }

    public static ValueTask<int> TmpName(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = Path.GetTempFileName();
        return new(1);
    }
}