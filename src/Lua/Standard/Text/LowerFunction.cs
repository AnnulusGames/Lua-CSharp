
namespace Lua.Standard.Text;

public sealed class LowerFunction : LuaFunction
{
    public override string Name => "lower";
    public static readonly LowerFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        buffer.Span[0] = s.ToLower();
        return new(1);
    }
}