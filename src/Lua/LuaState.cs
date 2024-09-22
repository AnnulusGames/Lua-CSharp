using System.Diagnostics.CodeAnalysis;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public sealed class LuaState
{
    public const string DefaultChunkName = "chunk";

    class GlobalState
    {
        public FastStackCore<LuaThread> threadStack;
        public FastListCore<UpValue> openUpValues;
        public readonly LuaTable environment;
        public readonly UpValue envUpValue;

        public GlobalState(LuaState state)
        {
            environment = new();
            envUpValue = UpValue.Closed(state, environment);
        }
    }

    readonly GlobalState globalState;

    readonly LuaStack stack = new();
    FastStackCore<CallStackFrame> callStack;
    bool isRunning;

    internal LuaStack Stack => stack;
    internal UpValue EnvUpValue => globalState.envUpValue;
    internal ref FastStackCore<LuaThread> ThreadStack => ref globalState.threadStack;

    public LuaTable Environment => globalState.environment;
    public bool IsRunning => Volatile.Read(ref isRunning);

    public static LuaState Create()
    {
        return new();
    }

    LuaState()
    {
        globalState = new(this);
    }

    LuaState(LuaState parent)
    {
        globalState = parent.globalState;
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

    public ReadOnlySpan<LuaValue> GetStackValues()
    {
        return stack.AsSpan();
    }

    public int StackCount => stack.Count;

    public void Push(LuaValue value)
    {
        ThrowIfRunning();
        stack.Push(value);
    }

    internal void PushCallStackFrame(CallStackFrame frame)
    {
        callStack.Push(frame);
    }

    internal void PopCallStackFrame()
    {
        var frame = callStack.Pop();
        stack.PopUntil(frame.Base);
    }

    internal ReadOnlySpan<CallStackFrame> GetCallStackSpan()
    {
        return callStack.AsSpan();
    }

    public LuaThread CreateThread(LuaFunction function, bool isProtectedMode = true)
    {
        return new LuaThread(new LuaState(this), function, isProtectedMode);
    }

    public bool TryGetCurrentThread([NotNullWhen(true)] out LuaThread? result)
    {
        return ThreadStack.TryPeek(out result);
    }

    public CallStackFrame GetCurrentFrame()
    {
        return callStack.Peek();
    }

    public Tracebacks GetTracebacks()
    {
        return new()
        {
            StackFrames = callStack.AsSpan()[1..].ToArray()
        };
    }

    internal UpValue GetOrAddUpValue(int registerIndex)
    {
        foreach (var upValue in globalState.openUpValues.AsSpan())
        {
            if (upValue.RegisterIndex == registerIndex && upValue.State == this)
            {
                return upValue;
            }
        }

        var newUpValue = UpValue.Open(this, registerIndex);
        globalState.openUpValues.Add(newUpValue);
        return newUpValue;
    }

    internal void CloseUpValues(int frameBase)
    {
        var openUpValues = globalState.openUpValues;
        for (int i = 0; i < openUpValues.Length; i++)
        {
            var upValue = openUpValues[i];
            if (upValue.State != this) continue;

            if (upValue.RegisterIndex >= frameBase)
            {
                upValue.Close();
                openUpValues.RemoveAtSwapback(i);
                i--;
            }
        }
    }

    internal void DumpStackValues()
    {
        var span = GetStackValues();
        for (int i = 0; i < span.Length; i++)
        {
            Console.WriteLine($"LuaStack [{i}]\t{span[i]}");
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
