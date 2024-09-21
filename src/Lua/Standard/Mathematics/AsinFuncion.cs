
namespace Lua.Standard.Mathematics;

public sealed class AsinFunction : LuaFunction
{
    public static readonly AsinFunction Instance = new();

    public override string Name => "asin";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Asin(arg0);
        return new(1);
    }
}