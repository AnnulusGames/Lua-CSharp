
namespace Lua.Standard.Mathematics;

public sealed class ExpFunction : LuaFunction
{
    public static readonly ExpFunction Instance = new();

    public override string Name => "exp";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Exp(arg0);
        return new(1);
    }
}