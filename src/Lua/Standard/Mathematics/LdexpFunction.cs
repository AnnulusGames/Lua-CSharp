
namespace Lua.Standard.Mathematics;

public sealed class LdexpFunction : LuaFunction
{
    public static readonly LdexpFunction Instance = new();

    public override string Name => "ldexp";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);

        buffer.Span[0] = arg0 * Math.Pow(2, arg1);
        return new(1);
    }
}