namespace Lua.Standard.OperatingSystem;

public sealed class RenameFunction : LuaFunction
{
    public override string Name => "rename";
    public static readonly RenameFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var oldName = context.GetArgument<string>(0);
        var newName = context.GetArgument<string>(1);
        try
        {
            File.Move(oldName, newName);
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