namespace Lua.Standard;

public static class CoroutineLibrary
{
    public static void OpenCoroutineLibrary(this LuaState state)
    {
        var coroutine = new LuaTable(0, Functions.Length);
        foreach (var func in Functions)
        {
            coroutine[func.Name] = func;
        }

        state.Environment["coroutine"] = coroutine;
    }

    static readonly LuaFunction[] Functions = [
        new("create", Create),
        new("resume", Resume),
        new("running", Running),
        new("wrap", Wrap),
        new("yield", Yield),
    ];

    public static ValueTask<int> Create(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);
        buffer.Span[0] = new LuaCoroutine(arg0, true);
        return new(1);
    }

    public static ValueTask<int> Resume(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = context.GetArgument<LuaThread>(0);
        return thread.ResumeAsync(context, buffer, cancellationToken);
    }

    public static ValueTask<int> Running(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = context.Thread;
        buffer.Span[1] = context.Thread == context.State.MainThread;
        return new(2);
    }

    public static ValueTask<int> Status(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = context.GetArgument<LuaThread>(0);
        buffer.Span[0] = thread.GetStatus() switch
        {
            LuaThreadStatus.Normal => "normal",
            LuaThreadStatus.Suspended => "suspended",
            LuaThreadStatus.Running => "running",
            LuaThreadStatus.Dead => "dead",
            _ => "",
        };
        return new(1);
    }

    public static ValueTask<int> Wrap(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);
        var thread = new LuaCoroutine(arg0, false);

        buffer.Span[0] = new LuaFunction("wrap", async (context, buffer, cancellationToken) =>
        {
            var stack = context.Thread.Stack;
            var frameBase = stack.Count;

            stack.Push(thread);
            stack.PushRange(context.Arguments);

            var resultCount = await thread.ResumeAsync(context with
            {
                ArgumentCount = context.ArgumentCount + 1,
                FrameBase = frameBase,
            }, buffer, cancellationToken);

            buffer.Span[1..].CopyTo(buffer.Span[0..]);
            return resultCount - 1;
        });

        return new(1);
    }

    public static ValueTask<int> Yield(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        return context.Thread.YieldAsync(context, buffer, cancellationToken);
    }
}