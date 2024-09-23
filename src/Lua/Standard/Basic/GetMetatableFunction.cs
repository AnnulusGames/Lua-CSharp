
using Lua.Runtime;

namespace Lua.Standard.Base;

public sealed class GetMetatableFunction : LuaFunction
{
    public override string Name => "getmetatable";
    public static readonly GetMetatableFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument(0);
        
        if (arg0.TryRead<LuaTable>(out var table))
        {
            if (table.Metatable == null)
            {
                buffer.Span[0] = LuaValue.Nil;
            }
            else if (table.Metatable.TryGetValue(Metamethods.Metatable, out var metatable))
            {
                buffer.Span[0] = metatable;
            }
            else
            {
                buffer.Span[0] = table.Metatable;
            }
        }
        else
        {
            buffer.Span[0] = LuaValue.Nil;
        }

        return new(1);
    }
}