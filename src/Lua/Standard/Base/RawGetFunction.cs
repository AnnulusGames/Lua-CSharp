
namespace Lua.Standard.Base;

public sealed class RawGetFunction : LuaFunction
{
    public override string Name => "rawget";
    public static readonly RawGetFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = ReadArgument<LuaTable>(context, 0);
        var arg1 = ReadArgument(context, 1);
        
        buffer.Span[0] = arg0[arg1];
        return new(1);
    }
}