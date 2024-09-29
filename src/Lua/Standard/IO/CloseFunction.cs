namespace Lua.Standard.IO;

public sealed class CloseFunction : LuaFunction
{
    public override string Name => "close";
    public static readonly CloseFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.ArgumentCount >= 1
            ? context.ReadArgument<FileHandle>(0)
            : context.State.Environment["io"].Read<LuaTable>()["stdout"].Read<FileHandle>();

        try
        {
            file.Close();
            buffer.Span[0] = true;
            return new(1);
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }
    }
}