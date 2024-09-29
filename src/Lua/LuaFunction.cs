using Lua.Runtime;

namespace Lua;

public abstract partial class LuaFunction
{
    public virtual string Name => GetType().Name;

    public async ValueTask<int> InvokeAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var state = context.State;
        var thread = state.CurrentThread;

        var frame = new CallStackFrame
        {
            Base = context.StackPosition == null ? thread.Stack.Count - context.ArgumentCount : context.StackPosition.Value,
            CallPosition = context.SourcePosition,
            ChunkName = context.ChunkName ?? LuaState.DefaultChunkName,
            RootChunkName = context.RootChunkName ?? LuaState.DefaultChunkName,
            VariableArgumentCount = this is Closure closure ? context.ArgumentCount - closure.Proto.ParameterCount : 0,
            Function = this,
        };

        thread.PushCallStackFrame(frame);
        try
        {
            return await InvokeAsyncCore(context, buffer, cancellationToken);
        }
        finally
        {
            thread.PopCallStackFrame();
        }
    }

    protected abstract ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken);
}