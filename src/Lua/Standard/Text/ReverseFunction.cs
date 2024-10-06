using Lua.Internal;

namespace Lua.Standard.Text;

public sealed class ReverseFunction : LuaFunction
{
    public override string Name => "reverse";
    public static readonly ReverseFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        using var strBuffer = new PooledArray<char>(s.Length);
        var span = strBuffer.AsSpan()[..s.Length];
        s.AsSpan().CopyTo(span);
        span.Reverse();
        buffer.Span[0] = span.ToString();
        return new(1);
    }
}