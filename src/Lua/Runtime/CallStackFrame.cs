using Lua.CodeAnalysis;

namespace Lua.Runtime;

public record struct CallStackFrame
{
    public required int Base;
    public required string ChunkName;
    public required string RootChunkName;
    public required SourcePosition? CallPosition;
    public required int VariableArgumentCount;
}