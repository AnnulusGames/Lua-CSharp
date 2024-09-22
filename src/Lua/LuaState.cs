using System.Diagnostics.CodeAnalysis;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public sealed class LuaState
{
    public const string DefaultChunkName = "chunk";

    LuaStack stack = new();
    FastStackCore<CallStackFrame> callStack;
    FastListCore<UpValue> openUpValues;

    LuaTable environment;
    internal UpValue EnvUpValue { get; }
    bool isRunning;

    internal LuaStack Stack => stack;

    public LuaTable Environment => environment;
    public bool IsRunning => Volatile.Read(ref isRunning);

    public static LuaState Create()
    {
        return new();
    }

    internal LuaState CreateCoroutineState()
    {
        return new LuaState(this);
    }

    LuaState()
    {
        environment = new();
        EnvUpValue = UpValue.Closed(environment);
    }

    LuaState(LuaState parent)
    {
        environment = parent.Environment;
        EnvUpValue = parent.EnvUpValue;
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

    public bool TryGetCurrentThread([NotNullWhen(true)] out LuaThread? result)
    {
        var span = GetCallStackSpan();
        
        for (int i = 0; i < span.Length; i++)
        {
            result = span[i].Function.Thread;
            if (result != null) return true;
        }

        result = default;
        return false;
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
        foreach (var upValue in openUpValues.AsSpan())
        {
            if (upValue.RegisterIndex == registerIndex)
            {
                return upValue;
            }
        }

        var newUpValue = UpValue.Open(registerIndex);
        openUpValues.Add(newUpValue);
        return newUpValue;
    }

    internal void CloseUpValues(int frameBase)
    {
        for (int i = 0; i < openUpValues.Length; i++)
        {
            var upValue = openUpValues[i];
            if (upValue.RegisterIndex >= frameBase)
            {
                upValue.Close(this);
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
