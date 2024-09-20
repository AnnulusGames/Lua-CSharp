namespace Lua.Standard.Base;

public sealed class ErrorFunction : LuaFunction
{
    public const string Name = "error";
    public static readonly ErrorFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var obj = context.ArgumentCount == 0 || context.Arguments[0].Type is LuaValueType.Nil
            ? "(error object is a nil value)"
            : context.Arguments[0].ToString();

        throw new LuaRuntimeException(context.State.GetTracebacks(), obj!);
    }
}