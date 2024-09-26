namespace Lua.Standard.Table;

public sealed class PackFunction : LuaFunction
{
    public override string Name => "pack";
    public static readonly PackFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var table = new LuaTable(context.ArgumentCount, 1);

        var span = context.Arguments;
        for (int i = 0; i < span.Length; i++)
        {
            table[i + 1] = span[i];
        }
        table["n"] = span.Length;

        buffer.Span[0] = table;
        return new(1);
    }
}