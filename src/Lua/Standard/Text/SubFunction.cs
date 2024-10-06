
using Lua.Internal;

namespace Lua.Standard.Text;

public sealed class SubFunction : LuaFunction
{
    public override string Name => "sub";
    public static readonly SubFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var i = context.GetArgument<double>(1);
        var j = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : -1;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, i);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 3, j);

        buffer.Span[0] = StringHelper.Slice(s, (int)i, (int)j).ToString();
        return new(1);
    }
}