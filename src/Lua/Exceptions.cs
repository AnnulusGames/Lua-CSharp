using Lua.CodeAnalysis;
using Lua.CodeAnalysis.Syntax;
using Lua.Runtime;

namespace Lua;

public class LuaException(string message) : Exception(message);

public class LuaParseException(string? chunkName, SourcePosition position, string message) : LuaException(message)
{
    public string? ChunkName { get; } = chunkName;
    public SourcePosition? Position { get; } = position;

    public static void UnexpectedToken(string? chunkName, SourcePosition position, SyntaxToken token)
    {
        throw new LuaParseException(chunkName, position, $"unexpected symbol <{token.Type}> near '{token.Text}'");
    }

    public static void ExpectedToken(string? chunkName, SourcePosition position, SyntaxTokenType token)
    {
        throw new LuaParseException(chunkName, position, $"'{token}' expected");
    }

    public static void UnfinishedLongComment(string? chunkName, SourcePosition position)
    {
        throw new LuaParseException(chunkName, position, $"unfinished long comment (starting at line {position.Line})");
    }

    public static void SyntaxError(string? chunkName, SourcePosition position, SyntaxToken? token)
    {
        throw new LuaParseException(chunkName, position, $"syntax error {(token == null ? "" : $"near '{token.Value.Text}'")}");
    }

    public static void NoVisibleLabel(string label, string? chunkName, SourcePosition position)
    {
        throw new LuaParseException(chunkName, position, $"no visible label '{label}' for <goto>");
    }

    public static void BreakNotInsideALoop(string? chunkName, SourcePosition position)
    {
        throw new LuaParseException(chunkName, position, "<break> not inside a loop");
    }

    public override string Message => $"{ChunkName ?? "<anonymous.lua>"}:{(Position == null ? "" : $"{Position.Value}:")} {base.Message}";
}

public class LuaRuntimeException(Tracebacks tracebacks, string message) : LuaException(message)
{
    public Tracebacks Tracebacks { get; } = tracebacks;

    public static void AttemptInvalidOperation(Tracebacks tracebacks, string op, LuaValue a, LuaValue b)
    {
        throw new LuaRuntimeException(tracebacks, $"attempt to {op} a '{a.Type}' with a '{b.Type}'");
    }

    public static void AttemptInvalidOperation(Tracebacks tracebacks, string op, LuaValue a)
    {
        throw new LuaRuntimeException(tracebacks, $"attempt to {op} a '{a.Type}' value");
    }

    public static void BadArgument(Tracebacks tracebacks, int argumentId, string functionName)
    {
        throw new LuaRuntimeException(tracebacks, $"bad argument #{argumentId} to '{functionName}' (value expected)");
    }

    public static void BadArgument(Tracebacks tracebacks, int argumentId, string functionName, LuaValueType[] expected)
    {
        throw new LuaRuntimeException(tracebacks, $"bad argument #{argumentId} to '{functionName}' ({string.Join(" or ", expected)} expected)");
    }

    public static void BadArgument(Tracebacks tracebacks, int argumentId, string functionName, string expected, string actual)
    {
        throw new LuaRuntimeException(tracebacks, $"bad argument #{argumentId} to '{functionName}' ({expected} expected, got {actual})");
    }

    public override string Message => $"{Tracebacks.RootChunkName}:{Tracebacks.LastPosition.Line}: {base.Message}{(Tracebacks.StackFrames.Length > 0 ? $"\n{Tracebacks}" : "")}";
}

public class LuaAssertionException(Tracebacks tracebacks, string message) : LuaRuntimeException(tracebacks, message)
{
    public override string ToString()
    {
        return $"{Message}\n{StackTrace}";
    }
}