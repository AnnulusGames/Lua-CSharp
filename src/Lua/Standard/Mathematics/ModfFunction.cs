
namespace Lua.Standard.Mathematics;

public sealed class ModfFunction : LuaFunction
{
    public static readonly ModfFunction Instance = new();

    public override string Name => "modf";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var (i, f) = MathEx.Modf(arg0);
        buffer.Span[0] = i;
        buffer.Span[1] = f;
        return new(2);
    }
}