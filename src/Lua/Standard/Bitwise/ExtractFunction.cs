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

        var n = (int)arg0;
        var field = (int)arg1;
        var width = (int)arg2;

        Bit32Helper.ValidateFieldAndWidth(context.State, this, 2, field, width);

        var result = (n >> field) & Bit32Helper.GetNBitMask(width);
        buffer.Span[0] = result;
        return new(1);
    }
}