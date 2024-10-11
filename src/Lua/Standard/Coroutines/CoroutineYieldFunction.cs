
namespace Lua.Standard.Coroutines;

public sealed class CoroutineYieldFunction : LuaFunction
{
    public static readonly CoroutineYieldFunction Instance = new();
    public override string Name => "yield";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        return context.Thread.Yield(context, buffer, cancellationToken);
    }
}