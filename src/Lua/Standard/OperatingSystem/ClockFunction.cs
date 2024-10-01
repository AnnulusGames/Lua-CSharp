
using System.Diagnostics;

namespace Lua.Standard.OperatingSystem;

public sealed class ClockFunction : LuaFunction
{
    public override string Name => "clock";
    public static readonly ClockFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = DateTimeHelper.GetUnixTime(DateTime.UtcNow, Process.GetCurrentProcess().StartTime);
        return new(1);
    }
}