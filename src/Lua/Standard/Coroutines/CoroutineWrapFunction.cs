
namespace Lua.Standard.Coroutines;

public sealed class CoroutineWrapFunction : LuaFunction
{
    public const string FunctionName = "wrap";

    public override string Name => FunctionName;

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<LuaFunction>(0);
        var thread = new LuaThread(context.State, arg0, false);
        buffer.Span[0] = new Wrapper(thread);
        return new(1);
    }

    class Wrapper(LuaThread targetThread) : LuaFunction
    {
        protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            return await targetThread.Resume(context, buffer, cancellationToken);
        }
    }
}