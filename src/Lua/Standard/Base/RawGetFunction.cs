
namespace Lua.Standard.Base;

public sealed class RawGetFunction : LuaFunction
{
    public const string Name = "rawget";
    public static readonly RawGetFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        ThrowIfArgumentNotExists(context, Name, 0);
        ThrowIfArgumentNotExists(context, Name, 1);

        var arg0 = context.Arguments[0];
        if (!arg0.TryRead<LuaTable>(out var table))
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 1, Name, LuaValueType.Table, arg0.Type);
        }

        buffer.Span[0] = table[context.Arguments[1]];
        return new(1);
    }
}