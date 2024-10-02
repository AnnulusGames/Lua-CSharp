namespace Lua.Standard.Bitwise;

public sealed class ArshiftFunction : LuaFunction
{
    public override string Name => "arshift";
    public static readonly ArshiftFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.GetArgument<double>(0);
        var disp = context.GetArgument<double>(1);

        if (!MathEx.IsInteger(disp))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'arshift' (number has no integer representation)");
        }

        var v = Bit32Helper.ToInt32(x);
        var a = (int)disp;

        if (a < 0)
        {
            v <<= -a;
        }
        else
        {
            v >>= a;
        }

        buffer.Span[0] = v;
        return new(1);
    }
}