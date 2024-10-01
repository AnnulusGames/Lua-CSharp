
namespace Lua.Standard.OperatingSystem;

// os.execute(command) is not supported (always return nil)

public sealed class ExecuteFunction : LuaFunction
{
    public override string Name => "execute";
    public static readonly SetLocaleFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (context.HasArgument(0))
        {
            throw new NotSupportedException("os.execute(command) is not supported");
        }
        else
        {
            buffer.Span[0] = false;
            return new(1);
        }
    }
}