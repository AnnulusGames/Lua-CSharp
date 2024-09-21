
namespace Lua.Standard.Mathematics;

public sealed class MaxFunction : LuaFunction
{
    public static readonly MaxFunction Instance = new();

    public override string Name => "max";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.ReadArgument<double>(0);
        for (int i = 1; i < context.ArgumentCount; i++)
        {
            x = Math.Max(x, context.ReadArgument<double>(i));
        }

        buffer.Span[0] = x;

        return new(1);
    }
}