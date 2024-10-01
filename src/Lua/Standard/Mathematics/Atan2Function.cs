
namespace Lua.Standard.Mathematics;

public sealed class Atan2Function : LuaFunction
{
    public static readonly Atan2Function Instance = new();

    public override string Name => "atan2";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);

        buffer.Span[0] = Math.Atan2(arg0, arg1);
        return new(1);
    }
}