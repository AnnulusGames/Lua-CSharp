using Lua.Internal;

namespace Lua.Runtime;

public sealed class Closure : LuaFunction
{
    Chunk proto;
    FastListCore<UpValue> upValues;

    public Closure(LuaState state, Chunk proto, LuaTable? environment = null)
    {
        this.proto = proto;

        // add upvalues
        for (int i = 0; i < proto.UpValues.Length; i++)
        {
            var description = proto.UpValues[i];
            var upValue = GetUpValueFromDescription(state, environment == null ? state.EnvUpValue : UpValue.Closed(environment), proto, description, 1);
            upValues.Add(upValue);
        }
    }

    public Chunk Proto => proto;
    public ReadOnlySpan<UpValue> UpValues => upValues.AsSpan();

    public override string Name => Proto.Name;

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        return LuaVirtualMachine.ExecuteClosureAsync(context.State, this, context.State.CurrentThread.GetCurrentFrame(), buffer, cancellationToken);
    }

    static UpValue GetUpValueFromDescription(LuaState state, UpValue envUpValue, Chunk proto, UpValueInfo description, int depth)
    {
        if (description.IsInRegister)
        {
            var thread = state.CurrentThread;
            var callStack = thread.GetCallStackFrames();
            var frame = callStack[^depth];
            return state.GetOrAddUpValue(thread, frame.Base + description.Index);
        }
        else if (description.Index == -1) // -1 is global environment
        {
            return envUpValue;
        }
        else
        {
            return GetUpValueFromDescription(state, envUpValue, proto.Parent!, proto.Parent!.UpValues[description.Index], depth + 1);
        }
    }
}