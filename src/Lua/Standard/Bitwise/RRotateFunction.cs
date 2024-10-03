namespace Lua.Standard.Bitwise;

public sealed class RRotateFunction : LuaFunction
{
    public override string Name => "rrotate";
    public static readonly RRotateFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.GetArgument<double>(0);
        var disp = context.GetArgument<double>(1);

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 1, x);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, disp);

        var v = Bit32Helper.ToUInt32(x);
        var a = ((int)disp) % 32;

        if (a < 0)
        {
            v = (v << (-a)) | (v >> (32 + a));
        }
        else
        {
            v = (v >> a) | (v << (32 - a));
        }

        buffer.Span[0] = v;
        return new(1);
    }
}