using Lua.Runtime;

namespace Lua;

public abstract partial class LuaFunction
{
    public virtual string Name => GetType().Name;

    public async ValueTask<int> InvokeAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var state = context.State;
        
        if (context.FrameBase == null)
        {
            context = context with
            {
                FrameBase = context.Thread.Stack.Count - context.ArgumentCount
            };
        }

        var frame = new CallStackFrame
        {
            Base = context.FrameBase.Value,
            CallPosition = context.SourcePosition,
            ChunkName = context.ChunkName ?? LuaState.DefaultChunkName,
            RootChunkName = context.RootChunkName ?? LuaState.DefaultChunkName,
            VariableArgumentCount = this is Closure closure ? Math.Max(context.ArgumentCount - closure.Proto.ParameterCount, 0) : 0,
            Function = this,
        };

        context.Thread.PushCallStackFrame(frame);
        try
        {
            return await InvokeAsyncCore(context, buffer, cancellationToken);
        }
        finally
        {
            context.Thread.PopCallStackFrame();
        }
    }

    protected abstract ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken);
}