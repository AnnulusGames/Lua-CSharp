using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lua.Runtime;

internal static class LuaRuntimeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetMetamethod(this LuaValue value, LuaState state, string methodName, out LuaValue result)
    {
        result = default;
        return state.TryGetMetatable(value, out var metatable) &&
            metatable.TryGetValue(methodName, out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVariableArgumentCount(this LuaFunction function, int argumentCount)
    {
        return function is Closure luaClosure
            ? argumentCount - luaClosure.Proto.ParameterCount
            : 0;
    }

#if NET6_0_OR_GREATER
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
#endif
    public static async ValueTask<int> InvokeAsync(this LuaFunction function, LuaFunctionExecutionContext context, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<LuaValue>.Shared.Rent(1024);
        buffer.AsSpan().Clear();
        try
        {
            return await function.InvokeAsync(context, buffer, cancellationToken);
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(buffer);
        }
    }
}