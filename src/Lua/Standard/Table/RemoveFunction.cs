namespace Lua.Standard.Table;

public sealed class RemoveFunction : LuaFunction
{
    public override string Name => "remove";
    public static readonly RemoveFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<LuaTable>(0);
        var arg1 = context.ArgumentCount >= 2
            ? context.ReadArgument<double>(1)
            : arg0.ArrayLength;

        if (!MathEx.IsInteger(arg1))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'remove' (number has no integer representation)");
        }

        buffer.Span[0] = arg0.RemoveAt((int)arg1);
        return new(1);
    }
}