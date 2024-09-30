
namespace Lua.Standard.Basic;

public sealed class RawSetFunction : LuaFunction
{
    public override string Name => "rawset";
    public static readonly RawSetFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaTable>(0);
        var arg1 = context.GetArgument(1);
        var arg2 = context.GetArgument(2);

        arg0[arg1] = arg2;
        return new(0);
    }
}