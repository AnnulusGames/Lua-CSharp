using Lua.Internal;

namespace Lua;

public static class LuaThreadExtensions
{
    public static async ValueTask<LuaValue[]> ResumeAsync(this LuaThread thread, LuaState state, CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledArray<LuaValue>(1024);

        var frameBase = thread.Stack.Count;
        thread.Stack.Push(thread);

        var resultCount = await thread.ResumeAsync(new()
        {
            State = state,
            Thread = state.CurrentThread,
            ArgumentCount = 1,
            FrameBase = frameBase,
        }, buffer.AsMemory(), cancellationToken);

        return buffer.AsSpan()[0..resultCount].ToArray();
    }

    public static async ValueTask<LuaValue[]> ResumeAsync(this LuaThread thread, LuaState state, LuaValue[] arguments, CancellationToken cancellationToken = default)
    {
        using var buffer = new PooledArray<LuaValue>(1024);

        var frameBase = thread.Stack.Count;
        thread.Stack.Push(thread);
        for (int i = 0; i < arguments.Length; i++)
        {
            thread.Stack.Push(arguments[i]);
        }

        var resultCount = await thread.ResumeAsync(new()
        {
            State = state,
            Thread = state.CurrentThread,
            ArgumentCount = 1 + arguments.Length,
            FrameBase = frameBase,
        }, buffer.AsMemory(), cancellationToken);

        return buffer.AsSpan()[0..resultCount].ToArray();
    }
}