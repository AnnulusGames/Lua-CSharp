
namespace Lua.Standard.Text;

public sealed class UpperFunction : LuaFunction
{
    public override string Name => "upper";
    public static readonly UpperFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        buffer.Span[0] = s.ToUpper();
        return new(1);
    }
}