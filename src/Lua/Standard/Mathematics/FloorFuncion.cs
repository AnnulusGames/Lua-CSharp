
namespace Lua.Standard.Mathematics;

public sealed class FloorFunction : LuaFunction
{
    public static readonly FloorFunction Instance = new();

    public override string Name => "floor";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Floor(arg0);
        return new(1);
    }
}