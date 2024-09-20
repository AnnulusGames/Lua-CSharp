
namespace Lua.Standard.Base;

public sealed class RawSetFunction : LuaFunction
{
    public const string Name = "rawset";
    public static readonly RawSetFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        ThrowIfArgumentNotExists(context, Name, 0);
        ThrowIfArgumentNotExists(context, Name, 1);
        ThrowIfArgumentNotExists(context, Name, 2);

        var arg0 = context.Arguments[0];
        if (!arg0.TryRead<LuaTable>(out var table))
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 1, Name, LuaValueType.Table, arg0.Type);
        }

        table[context.Arguments[1]] = context.Arguments[2];

        return new(0);
    }
}