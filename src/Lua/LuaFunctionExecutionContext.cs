using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Lua.CodeAnalysis;

namespace Lua;

public static class LuaFunctionExecutionContextPool
{
    static readonly ConcurrentStack<LuaFunctionExecutionContext> stack = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaFunctionExecutionContext Rent()
    {
        if (!stack.TryPop(out var context))
        {
            context = new();
        }

        return context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(LuaFunctionExecutionContext context)
    {
        context.State = default!;
        context.Thread = default!;
        context.ArgumentCount = default;
        context.FrameBase = default;
        context.SourcePosition = default;
        context.RootChunkName = default;
        context.ChunkName = default;
        stack.Push(context);
    }
}

public record LuaFunctionExecutionContext
{
    public LuaState State { get; set; } = default!;
    public LuaThread Thread { get; set; } = default!;
    public int ArgumentCount { get; set; }
    public int FrameBase { get; set; }
    public SourcePosition? SourcePosition { get; set; }
    public string? RootChunkName { get; set; }
    public string? ChunkName { get; set; }

    public ReadOnlySpan<LuaValue> Arguments
    {
        get
        {
            return Thread.GetStackValues().Slice(FrameBase, ArgumentCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasArgument(int index)
    {
        return ArgumentCount > index && Arguments[index].Type is not LuaValueType.Nil;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue GetArgument(int index)
    {
        ThrowIfArgumentNotExists(index);
        return Arguments[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetArgument<T>(int index)
    {
        ThrowIfArgumentNotExists(index);

        var arg = Arguments[index];
        if (!arg.TryRead<T>(out var argValue))
        {
            if (LuaValue.TryGetLuaValueType(typeof(T), out var type))
            {
                LuaRuntimeException.BadArgument(State.GetTraceback(), index + 1, Thread.GetCurrentFrame().Function.Name, type.ToString(), arg.Type.ToString());
            }
            else
            {
                LuaRuntimeException.BadArgument(State.GetTraceback(), index + 1, Thread.GetCurrentFrame().Function.Name, typeof(T).Name, arg.Type.ToString());
            }
        }

        return argValue;
    }

    void ThrowIfArgumentNotExists(int index)
    {
        if (ArgumentCount <= index)
        {
            LuaRuntimeException.BadArgument(State.GetTraceback(), index + 1, Thread.GetCurrentFrame().Function.Name);
        }
    }
}