
namespace Lua.Standard.Basic;

public sealed class RawEqualFunction : LuaFunction
{
    public override string Name => "rawequal";
    public static readonly RawEqualFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}