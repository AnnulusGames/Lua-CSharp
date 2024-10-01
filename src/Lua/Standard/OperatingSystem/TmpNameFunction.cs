namespace Lua.Standard.OperatingSystem;

public sealed class TmpNameFunction : LuaFunction
{
    public override string Name => "tmpname";
    public static readonly TmpNameFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = Path.GetTempFileName();
        return new(1);
    }
}