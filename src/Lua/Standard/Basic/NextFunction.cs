namespace Lua.Standard.Basic;

public sealed class NextFunction : LuaFunction
{
    public override string Name => "next";
    public static readonly NextFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaTable>(0);
        var arg1 = context.HasArgument(1) ? context.Arguments[1] : LuaValue.Nil;

        if (arg0.TryGetNext(arg1, out var kv))
        {
            buffer.Span[0] = kv.Key;
            buffer.Span[1] = kv.Value;
            return new(2);
        }
        else
        {
            buffer.Span[0] = LuaValue.Nil;
            return new(1);
        }
    }
}
