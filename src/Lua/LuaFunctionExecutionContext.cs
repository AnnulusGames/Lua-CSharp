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

    public int FrameBase => State.GetCurrentFrame().Base;
    public ReadOnlySpan<LuaValue> Arguments => State.GetStackValues().Slice(FrameBase, ArgumentCount);
}