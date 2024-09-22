
using System.Runtime.CompilerServices;

namespace Lua.Runtime;

public sealed class UpValue
{
    LuaValue value;

    public LuaState State { get; }
    public bool IsClosed { get; private set; }
    public int RegisterIndex { get; private set; }

    UpValue(LuaState state)
    {
        State = state;
    }

    public static UpValue Open(LuaState state, int registerIndex)
    {
        return new(state)
        {
            RegisterIndex = registerIndex
        };
    }

    public static UpValue Closed(LuaState state, LuaValue value)
    {
        return new(state)
        {
            IsClosed = true,
            value = value
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue GetValue()
    {
        if (IsClosed)
        {
            return value;
        }
        else
        {
            return State.Stack.UnsafeGet(RegisterIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(LuaValue value)
    {
        if (IsClosed)
        {
            this.value = value;
        }
        else
        {
            State.Stack.UnsafeGet(RegisterIndex) = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Close()
    {
        if (!IsClosed)
        {
            value = State.Stack.UnsafeGet(RegisterIndex);
        }

        IsClosed = true;
    }
}