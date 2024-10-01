namespace Lua.Standard.OperatingSystem;

public sealed class TimeFunction : LuaFunction
{
    public override string Name => "time";
    public static readonly TimeFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
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
}