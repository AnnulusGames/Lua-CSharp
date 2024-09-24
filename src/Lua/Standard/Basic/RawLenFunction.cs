
namespace Lua.Standard.Basic;

public sealed class RawLenFunction : LuaFunction
{
    public override string Name => "rawlen";
    public static readonly RawLenFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument(0);

        if (arg0.TryRead<LuaTable>(out var table))
        {
            buffer.Span[0] = table.ArrayLength;
        }
        else if (arg0.TryRead<string>(out var str))
        {
            buffer.Span[0] = str.Length;
        }
        else
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 2, Name, [LuaValueType.String, LuaValueType.Table]);
        }

        return new(1);
    }
}