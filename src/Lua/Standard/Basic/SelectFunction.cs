namespace Lua.Standard.Basic;

public sealed class SelectFunction : LuaFunction
{
    public override string Name => "select";
    public static readonly SelectFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);

        if (arg0.TryRead<double>(out var d))
        {
            if (!MathEx.IsInteger(d))
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #1 to 'select' (number has no integer representation)");
            }

            var index = (int)d;

            var span = context.Arguments[index..];
            span.CopyTo(buffer.Span);

            return new(span.Length);
        }
        else if (arg0.TryRead<string>(out var str) && str == "#")
        {
            buffer.Span[0] = context.ArgumentCount - 1;
            return new(1);
        }
        else
        {
            LuaRuntimeException.BadArgument(context.State.GetTraceback(), 1, Name, "number", arg0.Type.ToString());
            return default;
        }
    }
}
