using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lua.CodeAnalysis;

namespace Lua;

[StructLayout(LayoutKind.Auto)]
public readonly record struct LuaFunctionExecutionContext
{
    public required LuaState State { get; init; }
    public required LuaThread Thread { get; init; }
    public required int ArgumentCount { get; init; }
    public required int FrameBase { get; init; }
    public SourcePosition? SourcePosition { get; init; }
    public string? RootChunkName { get; init; }
    public string? ChunkName { get; init; }
    public int? CallerInstructionIndex { get; init; }
    public object? AdditionalContext { get; init; }

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
            var t = typeof(T);
            if ((t == typeof(int) || t == typeof(long)) && arg.TryReadNumber(out _))
            {
                LuaRuntimeException.BadArgumentNumberIsNotInteger(State.GetTraceback(), index + 1, Thread.GetCurrentFrame().Function.Name);
            }
            else if (LuaValue.TryGetLuaValueType(t, out var type))
            {
                LuaRuntimeException.BadArgument(State.GetTraceback(), index + 1, Thread.GetCurrentFrame().Function.Name, type.ToString(), arg.Type.ToString());
            }
            else
            {
                LuaRuntimeException.BadArgument(State.GetTraceback(), index + 1, Thread.GetCurrentFrame().Function.Name, t.Name, arg.Type.ToString());
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