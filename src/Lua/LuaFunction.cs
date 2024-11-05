using System.Runtime.CompilerServices;
using Lua.Runtime;

namespace Lua;

public class LuaFunction(string name, Func<LuaFunctionExecutionContext, Memory<LuaValue>, CancellationToken, ValueTask<int>> func)
{
    public string Name { get; } = name;
    internal Func<LuaFunctionExecutionContext, Memory<LuaValue>, CancellationToken, ValueTask<int>> Func { get; } = func;

    public LuaFunction(Func<LuaFunctionExecutionContext, Memory<LuaValue>, CancellationToken, ValueTask<int>> func) : this("anonymous", func)
    {
    }

    public async ValueTask<int> InvokeAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var frame = new CallStackFrame
        {
            Base = context.FrameBase,
            CallPosition = context.SourcePosition,
            ChunkName = context.ChunkName ?? LuaState.DefaultChunkName,
            RootChunkName = context.RootChunkName ?? LuaState.DefaultChunkName,
            VariableArgumentCount = this is Closure closure ? Math.Max(context.ArgumentCount - closure.Proto.ParameterCount, 0) : 0,
            Function = this,
        };

        context.Thread.PushCallStackFrame(frame);
        try
        {
            return await Func(context, buffer, cancellationToken);
        }
        finally
        {
            context.Thread.PopCallStackFrame();
        }
    }
    
    /// <summary>
    ///   Invokes the function without popping the call stack frame.
    ///   Pop the call stack frame  in Virtual Machine after the task is done.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal  ValueTask<int> InvokeAsyncPushOnly(in LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var frame = new CallStackFrame
        {
            Base = context.FrameBase,
            CallPosition = context.SourcePosition,
            ChunkName = context.ChunkName ?? LuaState.DefaultChunkName,
            RootChunkName = context.RootChunkName ?? LuaState.DefaultChunkName,
            VariableArgumentCount = this is Closure closure ? Math.Max(context.ArgumentCount - closure.Proto.ParameterCount, 0) : 0,
            Function = this,
        };

        context.Thread.PushCallStackFrame(in frame);
        return  Func(context, buffer, cancellationToken);
    }
}