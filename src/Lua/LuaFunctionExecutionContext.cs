using System.Runtime.CompilerServices;
using Lua.CodeAnalysis;

namespace Lua;

public readonly record struct LuaFunctionExecutionContext
{
    public required LuaState State { get; init; }
    public required int ArgumentCount { get; init; }
    public int? StackPosition { get; init; }
    public SourcePosition? SourcePosition { get; init; }
    public string? RootChunkName { get; init; }
    public string? ChunkName { get; init; }

    public ReadOnlySpan<LuaValue> Arguments
    {
        get
        {
            var thread = State.CurrentThread;
            return thread.GetStackValues().Slice(thread.GetCurrentFrame().Base, ArgumentCount);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue ReadArgument(int index)
    {
        ThrowIfArgumentNotExists(index);
        return Arguments[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadArgument<T>(int index)
    {
        ThrowIfArgumentNotExists(index);

        var arg = Arguments[index];
        if (!arg.TryRead<T>(out var argValue))
        {
            var thread = State.CurrentThread;
            if (LuaValue.TryGetLuaValueType(typeof(T), out var type))
            {
                LuaRuntimeException.BadArgument(State.GetTracebacks(), index + 1, thread.GetCurrentFrame().Function.Name, type.ToString(), arg.Type.ToString());
            }
            else
            {
                LuaRuntimeException.BadArgument(State.GetTracebacks(), index + 1, thread.GetCurrentFrame().Function.Name, typeof(T).Name, arg.Type.ToString());
            }
        }

        return argValue;
    }

    void ThrowIfArgumentNotExists(int index)
    {
        if (ArgumentCount <= index)
        {
            LuaRuntimeException.BadArgument(State.GetTracebacks(), index + 1, State.CurrentThread.GetCurrentFrame().Function.Name);
        }
    }
}