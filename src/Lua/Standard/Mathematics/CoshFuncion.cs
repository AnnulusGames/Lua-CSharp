
namespace Lua.Standard.Mathematics;

public sealed class CoshFunction : LuaFunction
{
    public static readonly CoshFunction Instance = new();

    public override string Name => "cosh";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Cosh(arg0);
        return new(1);
    }
}