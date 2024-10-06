using System.Text;
using System.Text.RegularExpressions;
using Lua.Internal;

namespace Lua.Standard.Text;

public sealed class GSubFunction : LuaFunction
{
    public override string Name => "gsub";
    public static readonly GSubFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var s = context.GetArgument<string>(0);
        var pattern = context.GetArgument<string>(1);
        var repl = context.GetArgument(2);
        var n_arg = context.HasArgument(3)
            ? context.GetArgument<double>(3)
            : int.MaxValue;

        LuaRuntimeException.ThrowBadArgumentIfNumberIsNotInteger(context.State, this, 4, n_arg);

        var n = (int)n_arg;
        var regex = StringHelper.ToRegex(pattern);
        var matches = regex.Matches(s);

        // TODO: reduce allocation
        var builder = new StringBuilder();
        var lastIndex = 0;
        var replaceCount = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            if (replaceCount > n) break;

            var match = matches[i];
            builder.Append(s.AsSpan()[lastIndex..match.Index]);
            replaceCount++;

            LuaValue result;
            if (repl.TryRead<string>(out var str))
            {
                result = str.Replace("%%", "%")
                    .Replace("%0", match.Value);

                for (int k = 1; k <= match.Groups.Count; k++)
                {
                    if (replaceCount > n) break;
                    result = result.Read<string>().Replace($"%{k}", match.Groups[k].Value);
                    replaceCount++;
                }
            }
            else if (repl.TryRead<LuaTable>(out var table))
            {
                result = table[match.Groups[1].Value];
            }
            else if (repl.TryRead<LuaFunction>(out var func))
            {
                for (int k = 1; k <= match.Groups.Count; k++)
                {
                    context.State.Push(match.Groups[k].Value);
                }

                using var methodBuffer = new PooledArray<LuaValue>(1024);
                await func.InvokeAsync(context with
                {
                    ArgumentCount = match.Groups.Count,
                    StackPosition = null
                }, methodBuffer.AsMemory(), cancellationToken);

                result = methodBuffer[0];
            }
            else
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #3 to 'gsub' (string/function/table expected)");
            }

            if (result.TryRead<string>(out var rs))
            {
                builder.Append(rs);
            }
            else if (result.TryRead<double>(out var rd))
            {
                builder.Append(rd);
            }
            else if (!result.ToBoolean())
            {
                builder.Append(match.Value);
                replaceCount--;
            }
            else
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), $"invalid replacement value (a {result.Type})");
            }

            lastIndex = match.Index + match.Length;
        }

        builder.Append(s.AsSpan()[lastIndex..s.Length]);

        buffer.Span[0] = builder.ToString();
        return 1;
    }
}