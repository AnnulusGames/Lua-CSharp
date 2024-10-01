namespace Lua.Standard.Table;

public sealed class UnpackFunction : LuaFunction
{
    public override string Name => "unpack";
    public static readonly UnpackFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaTable>(0);
        var arg1 = context.HasArgument(1)
            ? (int)context.GetArgument<double>(1)
            : 1;
        var arg2 = context.HasArgument(2)
            ? (int)context.GetArgument<double>(2)
            : arg0.ArrayLength;

        var index = 0;
        for (int i = arg1; i <= arg2; i++)
        {
            buffer.Span[index] = arg0[i];
            index++;
        }

        return new(index);
    }
}