namespace Lua.Standard.Text;

public sealed class StringIndexMetamethod(LuaTable table) : LuaFunction
{
    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        context.GetArgument<string>(0);
        var key = context.GetArgument(1);

        buffer.Span[0] = table[key];
        return new(1);
    }
}
