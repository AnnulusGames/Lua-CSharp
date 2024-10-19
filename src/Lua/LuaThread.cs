using System.Runtime.CompilerServices;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public abstract class LuaThread
{
    public abstract LuaThreadStatus GetStatus();
    public abstract void UnsafeSetStatus(LuaThreadStatus status);
    public abstract ValueTask<int> ResumeAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default);
    public abstract ValueTask<int> YieldAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default);

    LuaStack stack = new();
    FastStackCore<CallStackFrame> callStack;

    internal LuaStack Stack => stack;
    internal ref FastStackCore<CallStackFrame> CallStack => ref callStack;

    public CallStackFrame GetCurrentFrame()
    {
        return callStack.Peek();
    }

    public ReadOnlySpan<LuaValue> GetStackValues()
    {
        return stack.AsSpan();
    }

    public ReadOnlySpan<CallStackFrame> GetCallStackFrames()
    {
        return callStack.AsSpan();
    }

    internal void PushCallStackFrame(CallStackFrame frame)
    {
        callStack.Push(frame);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PushCallStackFrame(ref CallStackFrame frame)
    {
        callStack.Push(frame);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PopCallStackFrame()
    {
        var frame = callStack.Pop();
        stack.PopUntil(frame.Base);
    }

    internal void DumpStackValues()
    {
        var span = GetStackValues();
        for (int i = 0; i < span.Length; i++)
        {
            Console.WriteLine($"LuaStack [{i}]\t{span[i]}");
        }
    }
}