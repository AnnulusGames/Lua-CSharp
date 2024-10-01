using System.Runtime.CompilerServices;
using Lua.Runtime;

namespace Lua.Standard.OperatingSystem;

internal static class DateTimeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetUnixTime(DateTime dateTime)
    {
        return GetUnixTime(dateTime, DateTime.UnixEpoch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetUnixTime(DateTime dateTime, DateTime epoch)
    {
        var time = (dateTime - epoch).TotalSeconds;
        if (time < 0.0) return 0;
        return time;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime FromUnixTime(double unixTime)
    {
        var ts = TimeSpan.FromSeconds(unixTime);
        return DateTime.UnixEpoch + ts;
    }

    public static DateTime ParseTimeTable(LuaState state, LuaTable table)
    {
        static int GetTimeField(LuaState state, LuaTable table, string key, bool required = true, int defaultValue = 0)
        {
            if (!table.TryGetValue(key, out var value))
            {
                if (required)
                {
                    throw new LuaRuntimeException(state.GetTraceback(), $"field '{key}' missing in date table");
                }
                else
                {
                    return defaultValue;
                }
            }

            if (value.TryGetNumber(out var d) && MathEx.IsInteger(d))
            {
                return (int)d;
            }

            throw new LuaRuntimeException(state.GetTraceback(), $"field '{key}' is not an integer");
        }

        var day = GetTimeField(state, table, "day");
        var month = GetTimeField(state, table, "month");
        var year = GetTimeField(state, table, "year");
        var sec = GetTimeField(state, table, "sec", false, 0);
        var min = GetTimeField(state, table, "min", false, 0);
        var hour = GetTimeField(state, table, "hour", false, 12);

        return new DateTime(year, month, day, hour, min, sec);
    }
}