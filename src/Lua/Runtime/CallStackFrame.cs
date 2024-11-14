using System.Runtime.InteropServices;
using Lua.CodeAnalysis;

namespace Lua.Runtime;

[StructLayout(LayoutKind.Auto)]
public record struct CallStackFrame
{
    public required int Base;
    public required string ChunkName;
    public required string RootChunkName;
    public required LuaFunction Function;
    public required SourcePosition? CallPosition;
    public required int VariableArgumentCount;
    public  int? CallerInstructionIndex;
}