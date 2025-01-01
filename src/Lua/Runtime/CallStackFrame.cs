using System.Runtime.InteropServices;

namespace Lua.Runtime;

[StructLayout(LayoutKind.Auto)]
public record struct CallStackFrame
{
    public required int Base;
    public required LuaFunction Function;
    public required int VariableArgumentCount;
    public int CallerInstructionIndex;
    internal CallStackFrameFlags Flags;
}

[Flags]
public enum CallStackFrameFlags
{
    ReversedLe = 1,
}