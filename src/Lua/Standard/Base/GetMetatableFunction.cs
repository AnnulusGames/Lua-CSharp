
using Lua.Runtime;

namespace Lua.Standard.Base;

public sealed class GetMetatableFunction : LuaFunction
{
    public const string Name = "getmetatable";
    public static readonly GetMetatableFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        ThrowIfArgumentNotExists(context, Name, 0);

        var obj = context.Arguments[0];

        if (obj.TryRead<LuaTable>(out var table))
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