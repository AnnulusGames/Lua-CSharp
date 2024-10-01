
namespace Lua.Standard.Mathematics;

public sealed class TanFunction : LuaFunction
{
    public static readonly TanFunction Instance = new();

    public override string Name => "tan";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Tan(arg0);
        return new(1);
    }
}