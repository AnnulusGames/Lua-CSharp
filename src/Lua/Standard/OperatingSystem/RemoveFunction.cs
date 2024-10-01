namespace Lua.Standard.OperatingSystem;

public sealed class RemoveFunction : LuaFunction
{
    public override string Name => "remove";
    public static readonly RemoveFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var fileName = context.GetArgument<string>(0);
        try
        {
            File.Delete(fileName);
            buffer.Span[0] = true;
            return new(1);
        }
        catch(IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }
    }
}