namespace Lua;

public sealed class LuaMainThread : LuaThread
{
    public override LuaThreadStatus GetStatus()
    {
        return LuaThreadStatus.Running;
    }

    public override ValueTask<int> Resume(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        buffer.Span[0] = false;
        buffer.Span[1] = "cannot resume non-suspended coroutine";
        return new(2);
    }

    public override ValueTask Yield(LuaFunctionExecutionContext context, CancellationToken cancellationToken = default)
    {
        throw new LuaRuntimeException(context.State.GetTraceback(), "attempt to yield from outside a coroutine");
    }
}
