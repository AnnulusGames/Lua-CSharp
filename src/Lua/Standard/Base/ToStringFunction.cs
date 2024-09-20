
using System.Buffers;
using Lua.Runtime;

namespace Lua.Standard.Base;

public sealed class ToStringFunction : LuaFunction
{
    public const string Name = "tostring";
    public static readonly ToStringFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        ThrowIfArgumentNotExists(context, Name, 0);
        return ToStringCore(context, context.Arguments[0], buffer, cancellationToken);
    }

    internal static async ValueTask<int> ToStringCore(LuaFunctionExecutionContext context, LuaValue value, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (value.TryGetMetamethod(Metamethods.ToString, out var metamethod))
        {
            if (!metamethod.TryRead<LuaFunction>(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(context.State.GetTracebacks(), "call", metamethod);
            }

            context.State.Push(value);
            return await func.InvokeAsync(context with
            {
                ArgumentCount = 1,
            }, buffer, cancellationToken);
        }
        else
        {
            buffer.Span[0] = value.ToString()!;
            return 1;
        }
    }
}