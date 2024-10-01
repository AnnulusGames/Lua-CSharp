
namespace Lua.Standard.Mathematics;

public sealed class SqrtFunction : LuaFunction
{
    public static readonly SqrtFunction Instance = new();

    public override string Name => "sqrt";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Sqrt(arg0);
        return new(1);
    }
}