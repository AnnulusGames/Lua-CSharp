using System.Threading.Tasks.Sources;
using Lua.Internal;

namespace Lua;

public sealed class LuaCoroutine : LuaThread, IValueTaskSource<LuaCoroutine.YieldContext>, IValueTaskSource<LuaCoroutine.ResumeContext>
{
    struct YieldContext
    {
    }

    struct ResumeContext
    {
        public LuaValue[] Results;
    }

    byte status;
    bool isFirstCall = true;
    LuaState threadState;
    ValueTask<int> functionTask;

    ManualResetValueTaskSourceCore<ResumeContext> resume;
    ManualResetValueTaskSourceCore<YieldContext> yield;

    public override LuaThreadStatus GetStatus() => (LuaThreadStatus)status;

    public override void UnsafeSetStatus(LuaThreadStatus status)
    {
        this.status = (byte)status;
    }

    public bool IsProtectedMode { get; }
    public LuaFunction Function { get; }

    internal LuaCoroutine(LuaState state, LuaFunction function, bool isProtectedMode)
    {
        IsProtectedMode = isProtectedMode;
        threadState = state;
        Function = function;
    }

    public override async ValueTask<int> Resume(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        var baseThread = context.Thread;
        baseThread.UnsafeSetStatus(LuaThreadStatus.Normal);

        context.State.ThreadStack.Push(this);
        try
        {
            switch ((LuaThreadStatus)Volatile.Read(ref status))
            {
                case LuaThreadStatus.Suspended:
                    Volatile.Write(ref status, (byte)LuaThreadStatus.Running);

                    if (isFirstCall)
                    {
                        // copy stack value
                        Stack.EnsureCapacity(baseThread.Stack.Count);
                        baseThread.Stack.AsSpan().CopyTo(Stack.GetBuffer());
                        Stack.NotifyTop(baseThread.Stack.Count);

                        functionTask = Function.InvokeAsync(new()
                        {
                            State = threadState,
                            Thread = this,
                            ArgumentCount = context.ArgumentCount - 1,
                            ChunkName = Function.Name,
                            RootChunkName = context.RootChunkName,
                        }, buffer[1..], cancellationToken).Preserve();
                    }
                    else
                    {
                        yield.SetResult(new());
                    }
                    break;
                case LuaThreadStatus.Normal:
                case LuaThreadStatus.Running:
                    if (IsProtectedMode)
                    {
                        buffer.Span[0] = false;
                        buffer.Span[1] = "cannot resume non-suspended coroutine";
                        return 2;
                    }
                    else
                    {
                        throw new InvalidOperationException("cannot resume non-suspended coroutine");
                    }
                case LuaThreadStatus.Dead:
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

            var resumeTask = new ValueTask<ResumeContext>(this, resume.Version);

            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.UnsafeRegister(static x =>
                {
                    var coroutine = (LuaCoroutine)x!;
                    coroutine.yield.SetException(new OperationCanceledException());
                }, this);
            }

            try
            {
                if (isFirstCall)
                {
                    for (int i = 0; i < context.ArgumentCount - 1; i++)
                    {
                        threadState.Push(context.Arguments[i + 1]);
                    }

                    Volatile.Write(ref isFirstCall, false);
                }

                (var index, var result0, var result1) = await ValueTaskEx.WhenAny(resumeTask, functionTask!);

                if (index == 0)
                {
                    var results = result0.Results;

                    buffer.Span[0] = true;
                    for (int i = 0; i < results.Length; i++)
                    {
                        buffer.Span[i + 1] = results[i];
                    }

                    return results.Length + 1;
                }
                else
                {
                    Volatile.Write(ref status, (byte)LuaThreadStatus.Dead);
                    buffer.Span[0] = true;
                    return 1 + functionTask!.Result;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (IsProtectedMode)
                {
                    Volatile.Write(ref status, (byte)LuaThreadStatus.Dead);
                    buffer.Span[0] = false;
                    buffer.Span[1] = ex.Message;
                    return 2;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                registration.Dispose();
                resume.Reset();
            }
        }
        finally
        {
            context.State.ThreadStack.Pop();
            baseThread.UnsafeSetStatus(LuaThreadStatus.Running);
        }
    }

    public override async ValueTask Yield(LuaFunctionExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref status) != (byte)LuaThreadStatus.Running)
        {
            throw new InvalidOperationException("cannot call yield on a coroutine that is not currently running");
        }

        resume.SetResult(new()
        {
            Results = context.Arguments.ToArray(),
        });

        Volatile.Write(ref status, (byte)LuaThreadStatus.Suspended);

        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.UnsafeRegister(static x =>
            {
                var coroutine = (LuaCoroutine)x!;
                coroutine.yield.SetException(new OperationCanceledException());
            }, this);
        }

    RETRY:
        try
        {
            await new ValueTask<YieldContext>(this, yield.Version);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            yield.Reset();
            goto RETRY;
        }

        registration.Dispose();
        yield.Reset();
    }

    YieldContext IValueTaskSource<YieldContext>.GetResult(short token)
    {
        return yield.GetResult(token);
    }

    ValueTaskSourceStatus IValueTaskSource<YieldContext>.GetStatus(short token)
    {
        return yield.GetStatus(token);
    }

    void IValueTaskSource<YieldContext>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        yield.OnCompleted(continuation, state, token, flags);
    }

    ResumeContext IValueTaskSource<ResumeContext>.GetResult(short token)
    {
        return resume.GetResult(token);
    }

    ValueTaskSourceStatus IValueTaskSource<ResumeContext>.GetStatus(short token)
    {
        return resume.GetStatus(token);
    }

    void IValueTaskSource<ResumeContext>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        resume.OnCompleted(continuation, state, token, flags);
    }
}