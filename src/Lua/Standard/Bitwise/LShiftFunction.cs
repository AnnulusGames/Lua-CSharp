namespace Lua.Standard.Bitwise;

public sealed class LShiftFunction : LuaFunction
{
    public override string Name => "lshift";
    public static readonly LShiftFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.GetArgument<double>(0);
        var disp = context.GetArgument<double>(1);

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 1, x);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, disp);

        var v = Bit32Helper.ToUInt32(x);
        var a = (int)disp;

        if (Math.Abs(a) >= 32)
        {
            v = 0;
        }
        else if (a < 0)
        {
            v >>= -a;
        }
        else
        {
            v <<= a;
        }

        buffer.Span[0] = v;
        return new(1);
    }
}