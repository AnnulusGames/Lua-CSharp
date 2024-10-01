
namespace Lua.Standard.Mathematics;

public sealed class AbsFunction : LuaFunction
{
    public static readonly AbsFunction Instance = new();

    public override string Name => "abs";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Abs(arg0);
        return new(1);
    }
}