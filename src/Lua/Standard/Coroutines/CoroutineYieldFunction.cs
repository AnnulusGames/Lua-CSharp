
namespace Lua.Standard.Coroutines;

public sealed class CoroutineYieldFunction : LuaFunction
{
    public const string FunctionName = "yield";

    public override string Name => FunctionName;

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (!context.State.TryGetCurrentThread(out var thread))
        {
            throw new LuaRuntimeException(context.State.GetTracebacks(), "attempt to yield from outside a coroutine");
        }

        await thread.Yield(context, cancellationToken);

        return 0;
    }
}