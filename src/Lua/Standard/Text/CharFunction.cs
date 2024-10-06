
using System.Text;
using Lua.Internal;

namespace Lua.Standard.Text;

public sealed class CharFunction : LuaFunction
{
    public override string Name => "char";
    public static readonly CharFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (context.ArgumentCount == 0)
        {
            buffer.Span[0] = "";
            return new(1);
        }

        var builder = new ValueStringBuilder(context.ArgumentCount);
        for (int i = 0; i < context.ArgumentCount; i++)
        {
            var arg = context.GetArgument<double>(i);
            LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, i + 1, arg);
            builder.Append((char)arg);
        }

        buffer.Span[0] = builder.ToString();
        return new(1);
    }
}