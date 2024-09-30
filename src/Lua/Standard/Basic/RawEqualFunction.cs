
namespace Lua.Standard.Basic;

public sealed class RawEqualFunction : LuaFunction
{
    public override string Name => "rawequal";
    public static readonly RawEqualFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);
        var arg1 = context.GetArgument(1);

        buffer.Span[0] = arg0 == arg1;
        return new(1);
    }
}