
namespace Lua.Standard.Mathematics;

public sealed class CeilFunction : LuaFunction
{
    public static readonly CeilFunction Instance = new();

    public override string Name => "ceil";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Ceiling(arg0);
        return new(1);
    }
}