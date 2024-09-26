namespace Lua.Standard.IO;

public sealed class FileOpenFunction : LuaFunction
{
    public override string Name => "open";
    public static readonly FileOpenFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var fileName = context.ReadArgument<string>(0);
        var mode = context.ArgumentCount >= 2
            ? context.ReadArgument<string>(1)
            : "r";

        var fileMode = mode switch
        {
            "r" or "rb" or "r+" or "r+b" => FileMode.Open,
            "w" or "wb" or "w+" or "w+b" => FileMode.Create,
            "a" or "ab" or "a+" or "a+b" => FileMode.Append,
            _ => throw new LuaRuntimeException(context.State.GetTraceback(), "bad argument #2 to 'open' (invalid mode)"),
        };

        var fileAccess = mode switch
        {
            "r" or "rb" => FileAccess.Read,
            "w" or "wb" or "a" or "ab" => FileAccess.Write,
            _ => FileAccess.ReadWrite,
        };

        try
        {
            var stream = File.Open(fileName, fileMode, fileAccess);
            buffer.Span[0] = new FileHandle(stream);
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