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

        var resultCount = await function.InvokeAsync(new()
        {
            State = state,
            Thread = thread,
            ArgumentCount = arguments.Length,
            FrameBase = frameBase,
        }, buffer.AsMemory(), cancellationToken);

        return buffer.AsSpan()[0..resultCount].ToArray();
    }
}