
using System.Runtime.CompilerServices;

namespace Lua.Runtime;

public sealed class UpValue
{
    LuaValue value;

    public bool IsClosed { get; private set; }
    public int RegisterIndex { get; private set; }

    UpValue()
    {
    }

    public static UpValue Open(int registerIndex)
    {
        return new()
        {
            RegisterIndex = registerIndex
        };
    }

    public static UpValue Closed(LuaValue value)
    {
        return new()
        {
            IsClosed = true,
            value = value
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuaValue GetValue(LuaState state)
    {
        if (IsClosed)
        {
            return value;
        }
        else
        {
            return state.Stack.UnsafeGet(RegisterIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(LuaState state, LuaValue value)
    {
        if (IsClosed)
        {
            this.value = value;
        }
        else
        {
            state.Stack.UnsafeGet(RegisterIndex) = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Close(LuaState state)
    {
        if (!IsClosed)
        {
            value = state.Stack.UnsafeGet(RegisterIndex);
        }

        IsClosed = true;
    }
}