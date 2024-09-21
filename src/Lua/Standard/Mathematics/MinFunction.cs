
namespace Lua.Standard.Mathematics;

public sealed class MinFunction : LuaFunction
{
    public static readonly MinFunction Instance = new();

    public override string Name => "min";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.ReadArgument<double>(0);
        for (int i = 1; i < context.ArgumentCount; i++)
        {
            x = Math.Min(x, context.ReadArgument<double>(i));
        }

        buffer.Span[0] = x;

        return new(1);
    }
}