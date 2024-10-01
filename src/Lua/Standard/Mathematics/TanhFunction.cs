
namespace Lua.Standard.Mathematics;

public sealed class TanhFunction : LuaFunction
{
    public static readonly TanhFunction Instance = new();

    public override string Name => "tanh";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Tanh(arg0);
        return new(1);
    }
}