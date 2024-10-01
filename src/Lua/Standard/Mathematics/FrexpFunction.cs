
namespace Lua.Standard.Mathematics;

public sealed class FrexpFunction : LuaFunction
{
    public static readonly FrexpFunction Instance = new();

    public override string Name => "frexp";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);

        var (m, e) = MathEx.Frexp(arg0);
        buffer.Span[0] = m;
        buffer.Span[1] = e;
        return new(2);
    }
}