using System.Text.RegularExpressions;
using Lua.Internal;

namespace Lua.Standard.Text;

public sealed class GMatchFunction : LuaFunction
{
    public override string Name => "gmatch";
    public static readonly GMatchFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var pattern = context.GetArgument<string>(1);

        var regex = StringHelper.ToRegex(pattern);
        buffer.Span[0] = new Iterator(regex.Matches(s));
        return new(1);
    }

    class Iterator(MatchCollection matches) : LuaFunction
    {
        int i;

        protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            if (matches.Count > i)
            {
                var match = matches[i];
                var groups = match.Groups;

                i++;

                if (groups.Count == 1)
                {
                    buffer.Span[0] = match.Value;
                }
                else
                {
                    for (int j = 0; j < groups.Count; j++)
                    {
                        buffer.Span[j] = groups[j + 1].Value;
                    }
                }

                return new(groups.Count);
            }
            else
            {
                buffer.Span[0] = LuaValue.Nil;
                return new(1);
            }
        }
    }
}