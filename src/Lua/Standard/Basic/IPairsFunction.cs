using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class IPairsFunction : LuaFunction
{
    public override string Name => "ipairs";
    public static readonly IPairsFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaTable>(0);

        // If table has a metamethod __ipairs, calls it with table as argument and returns the first three results from the call.
        if (arg0.Metatable != null && arg0.Metatable.TryGetValue(Metamethods.IPairs, out var metamethod))
        {
            if (!metamethod.TryRead<LuaFunction>(out var function))
            {
                LuaRuntimeException.AttemptInvalidOperation(context.State.GetTraceback(), "call", metamethod);
            }

            return function.InvokeAsync(context, buffer, cancellationToken);
        }

        buffer.Span[0] = Iterator.Instance;
        buffer.Span[1] = arg0;
        buffer.Span[2] = 0;
        return new(3);
    }

    class Iterator : LuaFunction
    {
        public static readonly Iterator Instance = new();

        protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var table = context.GetArgument<LuaTable>(0);
            var i = context.GetArgument<double>(1);

            i++;
            if (table.TryGetValue(i, out var value))
            {
                buffer.Span[0] = i;
                buffer.Span[1] = value;
            }
            else
            {
                buffer.Span[0] = LuaValue.Nil;
                buffer.Span[1] = LuaValue.Nil;
            }

            return new(2);
        }
    }
}