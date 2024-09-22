namespace Lua;

public sealed class LuaThread
{
    LuaThreadStatus status;
    bool isProtectedMode;
    LuaState threadState;
    Task<int>? functionTask;

    TaskCompletionSource<LuaValue[]> resume = new();
    TaskCompletionSource<object?> yield = new();

    public LuaThreadStatus Status => status;
    public bool IsProtectedMode => isProtectedMode;
    public LuaFunction Function { get; }

    internal LuaThread(LuaState state, LuaFunction function, bool isProtectedMode)
    {
        this.isProtectedMode = isProtectedMode;
        threadState = state.CreateCoroutineState();
        Function = function;
        function.thread = this;
    }

    public async Task<int> Resume(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        if (status is LuaThreadStatus.Dead)
        {
            if (IsProtectedMode)
            {
                buffer.Span[0] = false;
                buffer.Span[1] = "cannot resume dead coroutine";
                return 2;
            }
            else
            {
                throw new InvalidOperationException("cannot resume dead coroutine");
            }
        }
        else if (status is LuaThreadStatus.Running)
        {
            throw new InvalidOperationException("cannot resume running coroutine");
        }

        if (status is LuaThreadStatus.Normal)
        {
            status = LuaThreadStatus.Running;

            // first argument is LuaThread object
            for (int i = 0; i < context.ArgumentCount - 1; i++)
            {
                threadState.Push(context.Arguments[i + 1]);
            }

            functionTask = Function.InvokeAsync(new()
            {
                State = threadState,
                ArgumentCount = context.ArgumentCount - 1,
                ChunkName = Function.Name,
                RootChunkName = context.RootChunkName,
            }, buffer[1..], cancellationToken).AsTask();
        }
        else
        {
            status = LuaThreadStatus.Running;

            if (cancellationToken.IsCancellationRequested)
            {
                yield.TrySetCanceled();
            }
            else
            {
                yield.TrySetResult(null);
            }
        }

        var resumeTask = resume.Task;
        var completedTask = await Task.WhenAny(resumeTask, functionTask!);

        if (!completedTask.IsCompletedSuccessfully)
        {
            if (IsProtectedMode)
            {
                status = LuaThreadStatus.Dead;
                buffer.Span[0] = false;
                buffer.Span[1] = completedTask.Exception.InnerException.Message;
                return 2;
            }
            else
            {
                throw completedTask.Exception.InnerException;
            }
        }

        if (completedTask == resumeTask)
        {
            resume = new();
            var results = resumeTask.Result;

            buffer.Span[0] = true;
            for (int i = 0; i < results.Length; i++)
            {
                buffer.Span[i + 1] = results[i];
            }

            return results.Length + 1;
        }
        else
        {
            status = LuaThreadStatus.Dead;
            buffer.Span[0] = true;
            return 1 + functionTask!.Result;
        }
    }

    public async Task Yield(LuaFunctionExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (status is not LuaThreadStatus.Running)
        {
            throw new InvalidOperationException("cannot call yield on a coroutine that is not currently running");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            resume.TrySetCanceled();
        }
        else
        {
            resume.TrySetResult(context.Arguments.ToArray());
        }

        status = LuaThreadStatus.Suspended;

RETRY:
        try
        {
            await yield.Task;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            yield = new();
            goto RETRY;
        }

        yield = new();
    }
}