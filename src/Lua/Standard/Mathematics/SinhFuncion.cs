
namespace Lua.Standard.Mathematics;
public sealed class SinhFunction : LuaFunction
{
    public static readonly SinhFunction Instance = new();

    public override string Name => "sinh";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Sinh(arg0);
        return new(1);
    }
}