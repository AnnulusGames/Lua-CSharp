
namespace Lua.Standard.Text;

// stirng.dump is not supported (throw exception)

public sealed class DumpFunction : LuaFunction
{
    public override string Name => "dump";
    public static readonly DumpFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("stirng.dump is not supported");
    }
}