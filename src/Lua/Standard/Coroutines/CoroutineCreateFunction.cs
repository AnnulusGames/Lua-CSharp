
namespace Lua.Standard.Coroutines;

public sealed class CoroutineCreateFunction : LuaFunction
{
    public const string FunctionName = "create";

    public override string Name => FunctionName;

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);
        buffer.Span[0] = context.State.CreateThread(arg0, true);
        return new(1);
    }
}