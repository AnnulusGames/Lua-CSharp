namespace Lua.Standard.Base;

public sealed class AssertFunction : LuaFunction
{
    public const string Name = "assert";
    public static readonly AssertFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        ThrowIfArgumentNotExists(context, Name, 0);

        if (!context.Arguments[0].ToBoolean())
        {
            var message = context.ArgumentCount >= 2
                ? context.Arguments[1].Read<string>()
                : $"assertion failed!";

            throw new LuaAssertionException(context.State.GetTracebacks(), message);
        }

        return new(0);
    }
}
