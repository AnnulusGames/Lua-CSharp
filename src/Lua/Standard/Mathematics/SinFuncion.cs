
namespace Lua.Standard.Mathematics;

public sealed class SinFunction : LuaFunction
{
    public static readonly SinFunction Instance = new();

    public override string Name => "sin";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Sin(arg0);
        return new(1);
    }
}