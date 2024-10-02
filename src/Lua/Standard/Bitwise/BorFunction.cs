namespace Lua.Standard.Bitwise;

public sealed class BorFunction : LuaFunction
{
    public override string Name => "bor";
    public static readonly BorFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (context.ArgumentCount == 0)
        {
            buffer.Span[0] = uint.MaxValue;
            return new(1);
        }

        var value = Bit32Helper.ToUInt32(context.GetArgument<double>(0));

        for (int i = 1; i < context.ArgumentCount; i++)
        {
            var v = Bit32Helper.ToUInt32(context.GetArgument<double>(i));
            value |= v;
        }

        buffer.Span[0] = value;
        return new(1);
    }
}