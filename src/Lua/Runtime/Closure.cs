using Lua.Internal;

namespace Lua.Runtime;

public sealed class Closure : LuaFunction
{
    Chunk proto;
    FastListCore<UpValue> upValues;

    public Closure(LuaState state, Chunk proto)
    {
        this.proto = proto;

        // add upvalues
        for (int i = 0; i < proto.UpValues.Length; i++)
        {
            var description = proto.UpValues[i];
            var upValue = GetUpValueFromDescription(state, proto, description);
            upValues.Add(upValue);
        }
    }

    public Chunk Proto => proto;
    public ReadOnlySpan<UpValue> UpValues => upValues.AsSpan();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        return LuaVirtualMachine.ExecuteClosureAsync(context.State, this, context.State.GetCurrentFrame(), buffer, cancellationToken);
    }

    static UpValue GetUpValueFromDescription(LuaState state, Chunk proto, UpValueInfo description)
    {
        if (description.IsInRegister)
        {
            return state.GetOrAddUpValue(state.GetCurrentFrame().Base + description.Index);
        }
        else if (description.Index == -1) // -1 is global environment
        {
            return state.EnvUpValue;
        }
        else
        {
            return GetUpValueFromDescription(state, proto.Parent!, proto.Parent!.UpValues[description.Index]);
        }
    }
}