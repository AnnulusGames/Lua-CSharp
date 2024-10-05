
using System.Text.RegularExpressions;
using Lua.Internal;

namespace Lua.Standard.Text;

public sealed class FindFunction : LuaFunction
{
    public override string Name => "find";
    public static readonly FindFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var pattern = context.GetArgument<string>(1);
        var init = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : 1;
        var plain = context.HasArgument(3)
            ? context.GetArgument<bool>(3)
            : false;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 3, init);

        // init can be negative value
        if (init < 0)
        {
            init = s.Length + init + 1;
        }

        var source = s.AsSpan()[(int)(init - 1)..];

        if (plain)
        {
            var start = source.IndexOf(pattern);
            if (start == -1)
            {
                buffer.Span[0] = LuaValue.Nil;
                return new(1);
            }

            // 1-based
            buffer.Span[0] = start + 1;
            buffer.Span[1] = start + pattern.Length;
            return new(2);
        }
        else
        {
            var regex = StringHelper.ToRegex(pattern);
            var match = regex.Match(source.ToString());

            if (match.Success)
            {
                // 1-based
                buffer.Span[0] = match.Index + 1;
                buffer.Span[1] = match.Index + match.Length;
                return new(2);
            }
            else
            {
                buffer.Span[0] = LuaValue.Nil;
                return new(1);
            }
        }
    }
}