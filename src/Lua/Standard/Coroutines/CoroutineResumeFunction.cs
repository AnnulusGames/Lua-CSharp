
namespace Lua.Standard.Coroutines;

public sealed class CoroutineResumeFunction : LuaFunction
{
    public const string FunctionName = "resume";

    public override string Name => FunctionName;

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = context.ReadArgument<LuaThread>(0);

        return await thread.Resume(context, buffer, cancellationToken);
    }
}