
namespace Lua.Standard.Mathematics;

public sealed class AcosFunction : LuaFunction
{
    public static readonly AcosFunction Instance = new();

    public override string Name => "acos";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        buffer.Span[0] = Math.Acos(arg0);
        return new(1);
    }
}