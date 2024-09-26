using System.Text;

namespace Lua.Standard.Table;

public sealed class ConcatFunction : LuaFunction
{
    public override string Name => "concat";
    public static readonly ConcatFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.ReadArgument<LuaTable>(0);
        var arg1 = context.ArgumentCount >= 2
            ? context.ReadArgument<string>(1)
            : "";
        var arg2 = context.ArgumentCount >= 3
            ? (int)context.ReadArgument<double>(2)
            : 1;
        var arg3 = context.ArgumentCount >= 4
            ? (int)context.ReadArgument<double>(3)
            : arg0.ArrayLength;

        var builder = new ValueStringBuilder(512);

        for (int i = arg2; i <= arg3; i++)
        {
            var value = arg0[i];

            if (value.Type is LuaValueType.String)
            {
                builder.Append(value.Read<string>());
            }
            else if (value.Type is LuaValueType.Number)
            {
                builder.Append(value.Read<double>().ToString());
            }
            else
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), $"invalid value ({value.Type}) at index {i} in table for 'concat'");
            }

            if (i != arg3) builder.Append(arg1);
        }

        buffer.Span[0] = builder.ToString();
        return new(1);
    }
}