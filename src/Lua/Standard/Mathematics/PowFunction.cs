
namespace Lua.Standard.Mathematics;

public sealed class PowFunction : LuaFunction
{
    public static readonly PowFunction Instance = new();

    public override string Name => "pow";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);

        buffer.Span[0] = Math.Pow(arg0, arg1);
        return new(1);
    }
}