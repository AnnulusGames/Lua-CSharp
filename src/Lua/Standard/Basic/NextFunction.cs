namespace Lua.Standard.Basic;

public sealed class NextFunction : LuaFunction
{
    public override string Name => "next";
    public static readonly NextFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<LuaTable>(0);
        var arg1 = context.ArgumentCount >= 2 ? context.Arguments[1] : LuaValue.Nil;

        var kv = arg0.GetNext(arg1);
        buffer.Span[0] = kv.Key;
        buffer.Span[1] = kv.Value;
        return new(2);
    }
}
