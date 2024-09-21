
namespace Lua.Standard.Mathematics;

public sealed class FmodFunction : LuaFunction
{
    public static readonly FmodFunction Instance = new();

    public override string Name => "fmod";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<double>(0);
        var arg1 = context.ReadArgument<double>(1);
        buffer.Span[0] = arg0 % arg1;
        return new(1);
    }
}