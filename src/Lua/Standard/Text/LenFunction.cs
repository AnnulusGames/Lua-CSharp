
namespace Lua.Standard.Text;

public sealed class LenFunction : LuaFunction
{
    public override string Name => "len";
    public static readonly LenFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        buffer.Span[0] = s.Length;
        return new(1);
    }
}