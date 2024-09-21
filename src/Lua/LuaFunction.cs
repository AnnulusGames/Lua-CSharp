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

    public virtual string Name => GetType().Name;
    protected abstract ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken);

    protected void ThrowIfArgumentNotExists(LuaFunctionExecutionContext context, int index)
    {
        if (context.ArgumentCount <= index)
        {
            LuaRuntimeException.BadArgument(context.State.GetTracebacks(), index + 1, Name);
        }
    }

    protected LuaValue ReadArgument(LuaFunctionExecutionContext context, int index)
    {
        ThrowIfArgumentNotExists(context, index);
        return context.Arguments[index];
    }

    protected T ReadArgument<T>(LuaFunctionExecutionContext context, int index)
    {
        ThrowIfArgumentNotExists(context, index);

        var arg = context.Arguments[index];
        if (!arg.TryRead<T>(out var argValue))
        {
            if (LuaValue.TryGetLuaValueType(typeof(T), out var type))
            {
                LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 1, Name, type.ToString(), arg.Type.ToString());
            }
            else
            {
                LuaRuntimeException.BadArgument(context.State.GetTracebacks(), 1, Name, typeof(T).Name, arg.Type.ToString());
            }
        }

        return argValue;
    }
}