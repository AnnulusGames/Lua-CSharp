using System.Runtime.CompilerServices;

namespace Lua.Standard.OperatingSystem;

internal static class DateTimeHelper
{
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
}