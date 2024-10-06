using System.Globalization;
using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class ToNumberFunction : LuaFunction
{
    public override string Name => "tonumber";
    public static readonly ToNumberFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);
        var arg1 = context.HasArgument(1)
            ? (int)context.GetArgument<double>(1)
            : 10;

        if (arg1 < 2 || arg1 > 36)
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'tonumber' (base out of range)");
        }

        if (arg0.Type is LuaValueType.Number)
        {
            buffer.Span[0] = arg0;
        }
        else if (arg0.TryRead<string>(out var str))
        {
            if (arg1 == 10 || arg1 == 16)
            {
                if (arg0.TryRead<double>(out var result))
                {
                    buffer.Span[0] = result;
                }
                else
                {
                    buffer.Span[0] = LuaValue.Nil;
                }
            }
            else if (arg0 == 10)
            {
                if (double.TryParse(str, out double result))
                {
                    buffer.Span[0] = result;
                }
                else
                {
                    buffer.Span[0] = LuaValue.Nil;
                }
            }
            else
            {
                try
                {
                    buffer.Span[0] = Convert.ToInt64(str, arg1);
                }
                catch (FormatException)
                {
                    buffer.Span[0] = LuaValue.Nil;
                }
            }
        }
        else
        {
            buffer.Span[0] = LuaValue.Nil;
        }

        return new(1);
    }
}