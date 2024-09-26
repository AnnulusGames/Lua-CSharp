namespace Lua.Standard.Table;

public sealed class RemoveFunction : LuaFunction
{
    public override string Name => "remove";
    public static readonly RemoveFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<LuaTable>(0);
        var arg1 = context.ArgumentCount >= 2
            ? (int)context.ReadArgument<double>(1)
            : arg0.ArrayLength;

        buffer.Span[0] = arg0.Remove(arg1);
        return new(1);
    }
}