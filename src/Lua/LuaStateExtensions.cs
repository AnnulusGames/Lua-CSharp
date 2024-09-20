using System.Buffers;
using Lua.Runtime;
using Lua.CodeAnalysis.Compilation;
using Lua.CodeAnalysis.Syntax;

namespace Lua;

public static class LuaStateExtensions
{
    public static ValueTask<int> RunAsync(this LuaState state, Chunk chunk, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        return new Closure(state, chunk).InvokeAsync(new()
        {
            State = state,
            ArgumentCount = 0,
            StackPosition = state.Stack.Count,
            SourcePosition = null,
            RootChunkName = chunk.Name ?? LuaState.DefaultChunkName,
            ChunkName = chunk.Name ?? LuaState.DefaultChunkName,
        }, buffer, cancellationToken);
    }

    public static ValueTask<int> DoStringAsync(this LuaState state, string source, Memory<LuaValue> buffer, string? chunkName = null, CancellationToken cancellationToken = default)
    {
        var syntaxTree = LuaSyntaxTree.Parse(source);
        var chunk = LuaCompiler.Default.Compile(syntaxTree, chunkName);
        return RunAsync(state, chunk, buffer, cancellationToken);
    }

    public static async ValueTask<LuaValue[]> DoStringAsync(this LuaState state, string source, string? chunkName = null, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<LuaValue>.Shared.Rent(1024);
        try
        {
            var resultCount = await DoStringAsync(state, source, buffer, chunkName, cancellationToken);
            return buffer.AsSpan(0, resultCount).ToArray();
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(buffer);
        }
    }

    public static async ValueTask<int> DoFileAsync(this LuaState state, string path, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var fileName = Path.GetFileName(path);
        var syntaxTree = LuaSyntaxTree.Parse(text, fileName);
        var chunk = LuaCompiler.Default.Compile(syntaxTree, fileName);
        return await RunAsync(state, chunk, buffer, cancellationToken);
    }

    public static async ValueTask<LuaValue[]> DoFileAsync(this LuaState state, string path, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<LuaValue>.Shared.Rent(1024);
        try
        {
            var resultCount = await DoFileAsync(state, path, buffer, cancellationToken);
            return buffer.AsSpan(0, resultCount).ToArray();
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(buffer);
        }
    }
}