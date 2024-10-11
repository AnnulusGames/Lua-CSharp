namespace Lua.Standard.Table;

public sealed class RemoveFunction : LuaFunction
{
    public override string Name => "remove";
    public static readonly RemoveFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var table = context.GetArgument<LuaTable>(0);
        var n_arg = context.HasArgument(1)
            ? context.GetArgument<double>(1)
            : table.ArrayLength;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, n_arg);

        var n = (int)n_arg;

        if (n <= 0 || n > table.GetArraySpan().Length)
        {
            if (!context.HasArgument(1) && n == 0)
            {
                buffer.Span[0] = LuaValue.Nil;
                return new(1);
            }
            
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'remove' (position out of bounds)");
        }
        else if (n > table.ArrayLength)
        {
            buffer.Span[0] = LuaValue.Nil;
            return new(1);
        }

        buffer.Span[0] = table.RemoveAt(n);
        return new(1);
    }
}