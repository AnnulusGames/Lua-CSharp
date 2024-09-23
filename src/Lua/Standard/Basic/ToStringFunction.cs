namespace Lua.Standard.Base;

public sealed class ToStringFunction : LuaFunction
{
    public override string Name => "tostring";
    public static readonly ToStringFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument(0);
        return arg0.CallToStringAsync(context, buffer, cancellationToken);
    }
}