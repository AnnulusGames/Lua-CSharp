
namespace Lua.Standard.Coroutines;

public sealed class CoroutineRunningFunction : LuaFunction
{
    public const string FunctionName = "running";

    public override string Name => FunctionName;

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = context.State.TryGetCurrentThread(out _);
        return new(1);
    }
}