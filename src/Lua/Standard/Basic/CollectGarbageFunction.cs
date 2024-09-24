namespace Lua.Standard.Basic;

public sealed class CollectGarbageFunction : LuaFunction
{
    public override string Name => "collectgarbage";
    public static readonly CollectGarbageFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        GC.Collect();
        return new(0);
    }
}