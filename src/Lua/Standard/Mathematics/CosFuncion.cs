
namespace Lua.Standard.Mathematics;

public sealed class CosFunction : LuaFunction
{
    public static readonly CosFunction Instance = new();

    public override string Name => "cos";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Cos(arg0);
        return new(1);
    }
}