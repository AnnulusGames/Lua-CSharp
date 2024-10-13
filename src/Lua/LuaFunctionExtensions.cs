using Lua.Internal;

namespace Lua;

public static class LuaFunctionExtensions
{
    public static async ValueTask<LuaValue[]> InvokeAsync(this LuaFunction function, LuaState state, LuaValue[] arguments, CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledArray<LuaValue>(1024);

        var thread = state.CurrentThread;
        var frameBase = thread.Stack.Count;
        
        for (int i = 0; i < arguments.Length; i++)
        {
            thread.Stack.Push(arguments[i]);
        }

        var funcContext = LuaFunctionExecutionContextPool.Rent();
        funcContext.State = state;
        funcContext.Thread = thread;
        funcContext.ArgumentCount = arguments.Length;
        funcContext.FrameBase = frameBase;
        funcContext.ChunkName = function.Name;
        funcContext.RootChunkName = function.Name;

        int resultCount;
        try
        {
            resultCount = await function.InvokeAsync(funcContext, buffer.AsMemory(), cancellationToken);
        }
        finally
        {
            LuaFunctionExecutionContextPool.Return(funcContext);
        }

        return buffer.AsSpan()[0..resultCount].ToArray();
    }
}