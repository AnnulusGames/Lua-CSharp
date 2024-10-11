
namespace Lua.Standard.Coroutines;

public sealed class CoroutineCreateFunction : LuaFunction
{
    public static readonly CoroutineCreateFunction Instance = new();
    public override string Name => "create";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);
        buffer.Span[0] = new LuaCoroutine(arg0, true);
        return new(1);
    }
}