
namespace Lua.Standard.Base;

public sealed class RawSetFunction : LuaFunction
{
    public override string Name => "rawset";
    public static readonly RawSetFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = ReadArgument<LuaTable>(context, 0);
        var arg1 = ReadArgument(context, 1);
        var arg2 = ReadArgument(context, 2);

        arg0[arg1] = arg2;
        return new(0);
    }
}