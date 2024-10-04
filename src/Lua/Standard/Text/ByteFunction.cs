
namespace Lua.Standard.Text;

public sealed class ByteFunction : LuaFunction
{
    public override string Name => "byte";
    public static readonly ByteFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var i = context.HasArgument(1)
            ? context.GetArgument<double>(1)
            : 1;
        var j = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : i;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, i);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 3, j);

        var span = StringHelper.Slice(s, (int)i, (int)j);
        for (int k = 0; k < span.Length; k++)
        {
            buffer.Span[k] = span[k];
        }

        return new(span.Length);
    }
}