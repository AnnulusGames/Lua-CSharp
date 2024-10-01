
namespace Lua.Standard.OperatingSystem;

// os.setlocale is not supported (always return nil)

public sealed class SetLocaleFunction : LuaFunction
{
    public override string Name => "setlocale";
    public static readonly SetLocaleFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = LuaValue.Nil;
        return new(1);
    }
}