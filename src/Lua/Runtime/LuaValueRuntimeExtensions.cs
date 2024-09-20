using System.Buffers;
using System.Runtime.CompilerServices;

namespace Lua.Runtime;

internal static class LuaRuntimeExtensions
{
    public static bool TryGetMetamethod(this LuaValue value, string methodName, out LuaValue result)
    {
        if (value.TryRead<LuaTable>(out var table) &&
            table.Metatable != null &&
            table.Metatable.TryGetValue(methodName, out result))
        {
            return true;
        }
        else
        {
            result = default;
            return false;
        }
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
            return await function.InvokeAsync(context, cancellationToken);
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(buffer);
        }
    }
}