
namespace Lua.Standard.Coroutines;

public sealed class CoroutineStatusFunction : LuaFunction
{
    public static readonly CoroutineStatusFunction Instance = new();
    public override string Name => "status";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = context.GetArgument<LuaThread>(0);
        buffer.Span[0] = thread.GetStatus() switch
        {
            LuaThreadStatus.Normal => "normal",
            LuaThreadStatus.Suspended => "suspended",
            LuaThreadStatus.Running => "running",
            LuaThreadStatus.Dead => "dead",
            _ => throw new NotImplementedException(),
        };
        return new(1);
    }
}