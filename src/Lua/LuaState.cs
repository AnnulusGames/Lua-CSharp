using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public sealed class LuaState
{
    public const string DefaultChunkName = "chunk";

    readonly LuaMainThread mainThread = new();
    FastListCore<UpValue> openUpValues;
    FastStackCore<LuaThread> threadStack;
    readonly LuaTable environment;
    readonly UpValue envUpValue;
    bool isRunning;

    internal UpValue EnvUpValue => envUpValue;
    internal ref FastStackCore<LuaThread> ThreadStack => ref threadStack;
    internal ref FastListCore<UpValue> OpenUpValues => ref openUpValues;

    public LuaTable Environment => environment;
    public LuaMainThread MainThread => mainThread;
    public LuaThread CurrentThread
    {
        get
        {
            if (threadStack.TryPeek(out var thread)) return thread;
            return mainThread;
        }
    }

    public static LuaState Create()
    {
        return new();
    }

    LuaState()
    {
        environment = new();
        envUpValue = UpValue.Closed(mainThread, environment);
    }

    public async ValueTask<int> RunAsync(Chunk chunk, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfRunning();

        Volatile.Write(ref isRunning, true);
        try
        {
            var closure = new Closure(this, chunk);
            return await closure.InvokeAsync(new()
            {
                State = this,
                ArgumentCount = 0,
                StackPosition = 0,
                SourcePosition = null,
                RootChunkName = chunk.Name ?? DefaultChunkName,
                ChunkName = chunk.Name ?? DefaultChunkName,
            }, buffer, cancellationToken);
        }
        finally
        {
            Volatile.Write(ref isRunning, false);
        }
    }

    public void Push(LuaValue value)
    {
        ThrowIfRunning();
        mainThread.Stack.Push(value);
    }

    public LuaThread CreateThread(LuaFunction function, bool isProtectedMode = true)
    {
        return new LuaCoroutine(this, function, isProtectedMode);
    }

    public Tracebacks GetTracebacks()
    {
        return MainThread.GetTracebacks();
    }

    internal UpValue GetOrAddUpValue(LuaThread thread, int registerIndex)
    {
        foreach (var upValue in openUpValues.AsSpan())
        {
            if (upValue.RegisterIndex == registerIndex && upValue.Thread == thread)
            {
                return upValue;
            }
        }

        var newUpValue = UpValue.Open(thread, registerIndex);
        openUpValues.Add(newUpValue);
        return newUpValue;
    }

    internal void CloseUpValues(LuaThread thread, int frameBase)
    {
        for (int i = 0; i < openUpValues.Length; i++)
        {
            var upValue = openUpValues[i];
            if (upValue.Thread != thread) continue;

            if (upValue.RegisterIndex >= frameBase)
            {
                upValue.Close();
                openUpValues.RemoveAtSwapback(i);
                i--;
            }
        }
    }

    void ThrowIfRunning()
    {
        if (Volatile.Read(ref isRunning))
        {
            throw new InvalidOperationException("the lua state is currently running");
        }
    }
}
