
namespace Lua.Standard.Mathematics;

public sealed class AtanFunction : LuaFunction
{
    public static readonly AtanFunction Instance = new();

    public override string Name => "atan";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Atan(arg0);
        return new(1);
    }
}