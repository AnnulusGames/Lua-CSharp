using System.Buffers;
using System.Threading.Tasks.Sources;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public sealed class LuaCoroutine : LuaThread, IValueTaskSource<LuaCoroutine.YieldContext>, IValueTaskSource<LuaCoroutine.ResumeContext>
{
    struct YieldContext
    {
        public required LuaValue[] Results;
    }

    struct ResumeContext
    {
        public required LuaValue[] Results;
    }

    byte status;
    bool isFirstCall = true;
    ValueTask<int> functionTask;
    LuaValue[] buffer;

    ManualResetValueTaskSourceCore<ResumeContext> resume;
    ManualResetValueTaskSourceCore<YieldContext> yield;

    public LuaCoroutine(LuaFunction function, bool isProtectedMode)
    {
        IsProtectedMode = isProtectedMode;
        Function = function;

        buffer = ArrayPool<LuaValue>.Shared.Rent(1024);
        buffer.AsSpan().Clear();
    }

    public override LuaThreadStatus GetStatus() => (LuaThreadStatus)status;

    public override void UnsafeSetStatus(LuaThreadStatus status)
    {
        this.status = (byte)status;
    }

    public bool IsProtectedMode { get; }
    public LuaFunction Function { get; }

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

                        // copy callstack value
                        CallStack.EnsureCapacity(baseThread.CallStack.Count);
                        baseThread.CallStack.AsSpan().CopyTo(CallStack.GetBuffer());
                        CallStack.NotifyTop(baseThread.CallStack.Count);
                    }
                    else
                    {
                        yield.SetResult(new()
                        {
                            Results = context.ArgumentCount == 1
                                ? []
                                : context.Arguments[1..].ToArray()
                        });
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
                    int frameBase;
                    var variableArgumentCount = Function.GetVariableArgumentCount(context.ArgumentCount - 1);
                    
                    if (variableArgumentCount > 0)
                    {
                        var fixedArgumentCount = context.ArgumentCount - 1 - variableArgumentCount;

                        for (int i = 0; i < variableArgumentCount; i++)
                        {
                            Stack.Push(context.GetArgument(i + fixedArgumentCount + 1));
                        }

                        frameBase = Stack.Count;

                        for (int i = 0; i < fixedArgumentCount; i++)
                        {
                            Stack.Push(context.GetArgument(i + 1));
                        }
                    }
                    else
                    {
                        frameBase = Stack.Count;

                        for (int i = 0; i < context.ArgumentCount - 1; i++)
                        {
                            Stack.Push(context.GetArgument(i + 1));
                        }
                    }

                    functionTask = Function.InvokeAsync(new()
                    {
                        State = context.State,
                        Thread = this,
                        ArgumentCount = context.ArgumentCount - 1,
                        FrameBase = frameBase,
                        ChunkName = Function.Name,
                        RootChunkName = context.RootChunkName,
                    }, this.buffer, cancellationToken).Preserve();

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
                    var resultCount = functionTask!.Result;

                    Volatile.Write(ref status, (byte)LuaThreadStatus.Dead);
                    buffer.Span[0] = true;
                    this.buffer[0..resultCount].CopyTo(buffer.Span[1..]);

                    ArrayPool<LuaValue>.Shared.Return(this.buffer);

                    return 1 + resultCount;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (IsProtectedMode)
                {
                    ArrayPool<LuaValue>.Shared.Return(this.buffer);

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

    public override async ValueTask<int> Yield(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
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
            var result = await new ValueTask<YieldContext>(this, yield.Version);
            for (int i = 0; i < result.Results.Length; i++)
            {
                buffer.Span[i] = result.Results[i];
            }

            return result.Results.Length;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            yield.Reset();
            goto RETRY;
        }
        finally
        {
            registration.Dispose();
            yield.Reset();
        }
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