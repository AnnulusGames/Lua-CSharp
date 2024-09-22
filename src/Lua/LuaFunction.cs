using Lua.Runtime;

namespace Lua;

public abstract partial class LuaFunction
{
    public virtual string Name => GetType().Name;

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
            Function = this,
        };

        state.PushCallStackFrame(frame);
        try
        {
            return await InvokeAsyncCore(context, buffer, cancellationToken);
        }
        catch (Exception ex) when (ex is not (LuaException or OperationCanceledException))
        {
            throw new LuaRuntimeException(state.GetTracebacks(), ex.Message);
        }
        finally
        {
            state.PopCallStackFrame();
        }
    }

    protected abstract ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken);
}