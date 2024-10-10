
namespace Lua.Standard.Coroutines;

public sealed class CoroutineRunningFunction : LuaFunction
{
    public static readonly CoroutineRunningFunction Instance = new();
    public override string Name => "running";


    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = context.Thread;
        buffer.Span[1] = context.Thread == context.State.MainThread;
        return new(2);
    }
}