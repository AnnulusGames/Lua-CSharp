using System.Buffers;
using System.Runtime.CompilerServices;
using Lua.Internal;

namespace Lua.Runtime;

internal static class LuaRuntimeExtensions
{
    public static bool TryGetNumber(this LuaValue value, out double result)
    {
        if (value.TryRead(out result)) return true;

        if (value.TryRead<string>(out var str))
        {
            var span = str.AsSpan().Trim();

            var sign = 1;
            if (span.Length > 0 && span[0] == '-')
            {
                sign = -1;
                span = span[1..];
            }

            if (span.Length > 2 && span[0] is '0' && span[1] is 'x' or 'X')
            {
                // TODO: optimize
                try
                {
                    result = HexConverter.ToDouble(span) * sign;
                    return true;
                }
                catch (FormatException)
                {
                    return false;
                }
            }
            else
            {
                return double.TryParse(str, out result);
            }
        }

        result = default;
        return false;
    }

    public static bool TryGetMetamethod(this LuaValue value, LuaState state, string methodName, out LuaValue result)
    {
        result = default;
        return state.TryGetMetatable(value, out var metatable) &&
            metatable.TryGetValue(methodName, out result);
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