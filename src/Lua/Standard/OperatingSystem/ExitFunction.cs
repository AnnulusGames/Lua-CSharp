
using Lua.Runtime;

namespace Lua.Standard.OperatingSystem;

public sealed class ExitFunction : LuaFunction
{
    public override string Name => "exit";
    public static readonly ExitFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        // Ignore 'close' parameter

        if (context.HasArgument(0))
        {
            var code = context.Arguments[0];

            if (code.TryRead<bool>(out var b))
            {
                Environment.Exit(b ? 0 : 1);
            }
            else if (code.TryRead<double>(out var d))
            {
                if (!MathEx.IsInteger(d))
                {
                    throw new LuaRuntimeException(context.State.GetTraceback(), $"bad argument #1 to 'exit' (number has no integer representation)");
                }

                Environment.Exit((int)d);
            }
            else
            {
                LuaRuntimeException.BadArgument(context.State.GetTraceback(), 1, Name, LuaValueType.Nil.ToString(), code.Type.ToString());
            }
        }
        else
        {
            Environment.Exit(0);
        }

        return new(0);
    }
}