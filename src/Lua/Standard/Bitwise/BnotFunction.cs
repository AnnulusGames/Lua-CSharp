namespace Lua.Standard.Bitwise;

public sealed class BnotFunction : LuaFunction
{
    public override string Name => "bnot";
    public static readonly BnotFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var value = Bit32Helper.ToUInt32(context.GetArgument<double>(0));
        buffer.Span[0] = ~value;
        return new(1);
    }
}