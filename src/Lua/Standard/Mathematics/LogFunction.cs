
namespace Lua.Standard.Mathematics;

public sealed class LogFunction : LuaFunction
{
    public static readonly LogFunction Instance = new();

    public override string Name => "log";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);

        if (context.ArgumentCount == 1)
        {
            buffer.Span[0] = Math.Log(arg0);
        }
        else
        {
            var arg1 = context.GetArgument<double>(1);
            buffer.Span[0] = Math.Log(arg0, arg1);
        }

        return new(1);
    }
}