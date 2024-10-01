
using System.Diagnostics;

namespace Lua.Standard.OperatingSystem;

public sealed class DiffTimeFunction : LuaFunction
{
    public override string Name => "difftime";
    public static readonly DiffTimeFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var t2 = context.GetArgument<double>(0);
        var t1 = context.GetArgument<double>(1);
        buffer.Span[0] = t2 - t1;
        return new(1);
    }
}