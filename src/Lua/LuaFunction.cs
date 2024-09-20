using Lua.Runtime;

namespace Lua;

public abstract partial class LuaFunction
{
    public async ValueTask<int> InvokeAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var state = context.State;

        var frame = new CallStackFrame
        {
            Base = context.StackPosition == null ? state.Stack.Count - context.ArgumentCount : context.StackPosition.Value,
            CallPosition = context.SourcePosition,
            ChunkName = context.ChunkName ?? LuaState.DefaultChunkName,
            RootChunkName = context.RootChunkName ?? LuaState.DefaultChunkName,
            VariableArgumentCount = this is Closure closure ? context.ArgumentCount - closure.Proto.ParameterCount : 0,
        };

        state.PushCallStackFrame(frame);
        try
        {
            return await InvokeAsyncCore(context, buffer, cancellationToken);
        }
        finally
        {
            state.PopCallStackFrame();
        }
    }

    protected abstract ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken);

    protected static void ThrowIfArgumentNotExists(LuaFunctionExecutionContext context, string chunkName, int index)
    {
        if (context.ArgumentCount == index)
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), index + 1, chunkName);
        }
    }
}