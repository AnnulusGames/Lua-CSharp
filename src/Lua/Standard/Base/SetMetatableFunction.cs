
using Lua.Runtime;

namespace Lua.Standard.Base;

public sealed class SetMetatableFunction : LuaFunction
{
    public const string Name = "setmetatable";
    public static readonly SetMetatableFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        ThrowIfArgumentNotExists(context, Name, 0);
        ThrowIfArgumentNotExists(context, Name, 1);

        var arg0 = context.Arguments[0];
        if (!arg0.TryRead<LuaTable>(out var table))
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 1, Name, LuaValueType.Table, arg0.Type);
        }

        var arg1 = context.Arguments[1];
        if (arg1.Type is not (LuaValueType.Nil or LuaValueType.Table))
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 2, Name, [LuaValueType.Nil, LuaValueType.Table]);
        }

        if (table.Metatable != null && table.Metatable.TryGetValue(Metamethods.Metatable, out _))
        {
            throw new LuaRuntimeException(context.State.GetTracebacks(), "cannot change a protected metatable");
        }
        else if (arg1.Type is LuaValueType.Nil)
        {
            table.Metatable = null;
        }
        else
        {
            table.Metatable = arg1.Read<LuaTable>();
        }

        buffer.Span[0] = table;
        return new(1);
    }
}