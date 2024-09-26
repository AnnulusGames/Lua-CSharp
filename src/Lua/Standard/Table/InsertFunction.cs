namespace Lua.Standard.Table;

public sealed class InsertFunction : LuaFunction
{
    public override string Name => "insert";
    public static readonly InsertFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var table = context.ReadArgument<LuaTable>(0);

        var value = context.ArgumentCount >= 3
            ? context.ReadArgument(2)
            : context.ReadArgument(1);

        var pos = context.ArgumentCount >= 3
            ? context.ReadArgument<double>(1)
            : table.ArrayLength + 1;

        if (!MathEx.IsInteger(pos))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'insert' (number has no integer representation)");
        }

        if (pos <= 0 || pos > table.ArrayLength + 1)
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'insert' (position out of bounds)");
        }

        table.Insert((int)pos, value);
        return new(0);
    }
}