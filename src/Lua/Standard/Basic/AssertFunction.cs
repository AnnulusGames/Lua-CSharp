namespace Lua.Standard.Basic;

public sealed class AssertFunction : LuaFunction
{
    public override string Name => "assert";
    public static readonly AssertFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);

        if (!arg0.ToBoolean())
        {
            var message = "assertion failed!";
            if (context.HasArgument(1))
            {
                message = context.GetArgument<string>(1);
            }

            throw new LuaAssertionException(context.State.GetTraceback(), message);
        }

        context.Arguments.CopyTo(buffer.Span);
        return new(context.ArgumentCount);
    }
}
