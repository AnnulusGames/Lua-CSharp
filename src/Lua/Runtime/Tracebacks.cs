using Lua.CodeAnalysis;

namespace Lua.Runtime;

public class Traceback
{
    public required CallStackFrame[] StackFrames { get; init; }

    internal string RootChunkName => StackFrames.Length == 0 ? "" : StackFrames[^1].RootChunkName;
    internal SourcePosition LastPosition => StackFrames.Length == 0 ? default : StackFrames[^1].CallPosition!.Value;

    public override string ToString()
    {
        var str = string.Join("\n   ", StackFrames
            .Where(x => x.CallPosition != null)
            .Select(x =>
            {
                return $"{x.RootChunkName}:{x.CallPosition!.Value.Line}: {(string.IsNullOrEmpty(x.ChunkName) ? "" : $"in '{x.ChunkName}'")}";
            })
            .Reverse());

        return $"stack traceback:\n   {str}";
    }
}