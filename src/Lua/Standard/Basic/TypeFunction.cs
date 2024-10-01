namespace Lua.Standard.Basic;

public sealed class TypeFunction : LuaFunction
{
    public override string Name => "type";
    public static readonly TypeFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);

        buffer.Span[0] = arg0.Type switch
        {
            LuaValueType.Nil => "nil",
            LuaValueType.Boolean => "boolean",
            LuaValueType.String => "string",
            LuaValueType.Number => "number",
            LuaValueType.Function => "function",
            LuaValueType.Thread => "thread",
            LuaValueType.UserData => "userdata",
            LuaValueType.Table => "table",
            _ => throw new NotImplementedException(),
        };

        return new(1);
    }
}
