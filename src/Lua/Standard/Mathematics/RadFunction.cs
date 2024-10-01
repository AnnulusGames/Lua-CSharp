
namespace Lua.Standard.Mathematics;

public sealed class RadFunction : LuaFunction
{
    public static readonly RadFunction Instance = new();

    public override string Name => "rad";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = arg0 * (Math.PI / 180.0);
        return new(1);
    }
}