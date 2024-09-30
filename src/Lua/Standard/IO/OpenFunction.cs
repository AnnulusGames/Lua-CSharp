namespace Lua.Standard.IO;

public sealed class OpenFunction : LuaFunction
{
    public override string Name => "open";
    public static readonly OpenFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var fileName = context.GetArgument<string>(0);
        var mode = context.HasArgument(1)
            ? context.GetArgument<string>(1)
            : "r";

        var resultCount = IOHelper.Open(context.State, fileName, mode, buffer, false);
        return new(resultCount);
    }
}