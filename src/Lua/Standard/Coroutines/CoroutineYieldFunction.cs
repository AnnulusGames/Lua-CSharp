
namespace Lua.Standard.Coroutines;

public sealed class CoroutineYieldFunction : LuaFunction
{
    public const string FunctionName = "yield";

    public override string Name => FunctionName;

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        await context.State.CurrentThread.Yield(context, cancellationToken);
        return 0;
    }
}