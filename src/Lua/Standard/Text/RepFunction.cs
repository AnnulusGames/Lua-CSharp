
using System.Text;

namespace Lua.Standard.Text;

public sealed class RepFunction : LuaFunction
{
    public override string Name => "rep";
    public static readonly RepFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var n_arg = context.GetArgument<double>(1);

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 2, n_arg);

        var n = (int)n_arg;

        var builder = new ValueStringBuilder(s.Length * n);
        for (int i = 0; i < n; i++)
        {
            builder.Append(s);
        }

        buffer.Span[0] = builder.ToString();
        return new(1);
    }
}