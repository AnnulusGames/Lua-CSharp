
namespace Lua.Standard.Coroutines;

public sealed class CoroutineResumeFunction : LuaFunction
{
    public static readonly CoroutineResumeFunction Instance = new();
    public override string Name => "resume";

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = context.GetArgument<LuaThread>(0);
        return await thread.ResumeAsync(context, buffer, cancellationToken);
    }
}