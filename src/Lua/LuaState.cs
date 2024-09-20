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

    internal LuaStack Stack => stack;

    public LuaTable Environment => environment;

    public static LuaState Create()
    {
        return new();
    }

    LuaState()
    {
        environment = new();
        EnvUpValue = UpValue.Closed(environment);
    }

    public ReadOnlySpan<LuaValue> GetStackValues()
    {
        return stack.AsSpan();
    }

    public void Push(LuaValue value)
    {
        stack.Push(value);
    }

    internal void Reset()
    {
        stack.Clear();
        callStack.Clear();
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

    public CallStackFrame GetCurrentFrame()
    {
        return callStack.Peek();
    }

    internal Tracebacks GetTracebacks()
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
}
