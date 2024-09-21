
namespace Lua.Standard.Mathematics;

public sealed class DegFunction : LuaFunction
{
    public static readonly DegFunction Instance = new();

    public override string Name => "deg";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = arg0 * (180.0 / Math.PI);
        return new(1);
    }
}