namespace Lua.Standard.Bitwise;

public sealed class ExtractFunction : LuaFunction
{
    public override string Name => "extract";
    public static readonly ExtractFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);
        var arg2 = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : 1;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 1, arg0);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, arg1);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 3, arg2);

        var n = Bit32Helper.ToUInt32(arg0);
        var field = (int)arg1;
        var width = (int)arg2;

        Bit32Helper.ValidateFieldAndWidth(context.State, this, 2, field, width);
        
        if (field == 0 && width == 32)
        {
            buffer.Span[0] = n;
        }
        else
        {
            var mask = (uint)((1 << width) - 1);
            buffer.Span[0] = (n >> field) & mask;
        }

        return new(1);
    }
}