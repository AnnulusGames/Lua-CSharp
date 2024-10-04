
using System.Text;

namespace Lua.Standard.Text;

public sealed class SubFunction : LuaFunction
{
    public override string Name => "sub";
    public static readonly SubFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var i_arg = context.GetArgument<double>(1);
        var j_arg = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : -1;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, i_arg);
        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 3, j_arg);

        var i = (int)i_arg;
        var j = (int)j_arg;
        buffer.Span[0] = StringHelper.SubString(s, i, j);
        return new(1);
    }
}