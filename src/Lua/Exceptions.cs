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

    public override string Message => $"{ChunkName}:{(Position == null ? "" : $"{Position.Value}:")} {base.Message}";
}

public class LuaRuntimeException(Traceback traceback, string message) : LuaException(message)
{
    public Traceback LuaTraceback { get; } = traceback;

    public static void AttemptInvalidOperation(Traceback traceback, string op, LuaValue a, LuaValue b)
    {
        throw new LuaRuntimeException(traceback, $"attempt to {op} a '{a.Type}' with a '{b.Type}'");
    }

    public static void AttemptInvalidOperation(Traceback traceback, string op, LuaValue a)
    {
        throw new LuaRuntimeException(traceback, $"attempt to {op} a '{a.Type}' value");
    }

    public static void BadArgument(Traceback traceback, int argumentId, string functionName)
    {
        throw new LuaRuntimeException(traceback, $"bad argument #{argumentId} to '{functionName}' (value expected)");
    }

    public static void BadArgument(Traceback traceback, int argumentId, string functionName, LuaValueType[] expected)
    {
        throw new LuaRuntimeException(traceback, $"bad argument #{argumentId} to '{functionName}' ({string.Join(" or ", expected)} expected)");
    }

    public static void BadArgument(Traceback traceback, int argumentId, string functionName, string expected, string actual)
    {
        throw new LuaRuntimeException(traceback, $"bad argument #{argumentId} to '{functionName}' ({expected} expected, got {actual})");
    }

    public static void ThrowBadArgumentIfNumberIsNotInteger(LuaState state, string functionName, int argumentId, double value)
    {
        if (!MathEx.IsInteger(value))
        {
            throw new LuaRuntimeException(state.GetTraceback(), $"bad argument #{argumentId} to '{functionName}' (number has no integer representation)");
        }
    }

    public override string Message => $"{LuaTraceback.RootChunkName}:{LuaTraceback.LastPosition.Line}: {base.Message}";

    public override string ToString()
    {
        return $"{Message}\n{(LuaTraceback.StackFrames.Length > 0 ? $"{LuaTraceback}\n" : "")}{StackTrace}";
    }
}

public class LuaAssertionException(Traceback traceback, string message) : LuaRuntimeException(traceback, message)
{
    public override string ToString()
    {
        return $"{Message}\n{StackTrace}";
    }
}

public class LuaModuleNotFoundException(string moduleName) : LuaException($"module '{moduleName}' not found");

public class LuaRuntimeCSharpException(Traceback traceback, Exception exception) : LuaRuntimeException(traceback, exception.Message)
{
    public Exception Exception { get; } = exception;
}