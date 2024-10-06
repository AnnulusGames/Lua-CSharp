
namespace Lua.Standard.Coroutines;

public sealed class CoroutineYieldFunction : LuaFunction
{
    public static readonly CoroutineYieldFunction Instance = new();
    public override string Name => "yield";

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        await context.State.CurrentThread.Yield(context, cancellationToken);
        return 0;
    }
}