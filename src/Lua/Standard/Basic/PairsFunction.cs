using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class PairsFunction : LuaFunction
{
    public override string Name => "pairs";
    public static readonly PairsFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaTable>(0);

        // If table has a metamethod __pairs, calls it with table as argument and returns the first three results from the call.
        if (arg0.Metatable != null && arg0.Metatable.TryGetValue(Metamethods.Pairs, out var metamethod))
        {
            if (!metamethod.TryRead<LuaFunction>(out var function))
            {
                LuaRuntimeException.AttemptInvalidOperation(context.State.GetTraceback(), "call", metamethod);
            }

            return function.InvokeAsync(context, buffer, cancellationToken);
        }

        buffer.Span[0] = NextFunction.Instance;
        buffer.Span[1] = arg0;
        buffer.Span[2] = LuaValue.Nil;
        return new(3);
    }
}