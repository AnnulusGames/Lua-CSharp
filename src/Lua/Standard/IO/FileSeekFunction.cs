namespace Lua.Standard.IO;

public sealed class FileSeekFunction : LuaFunction
{
    public override string Name => "seek";
    public static readonly FileSeekFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.GetArgument<FileHandle>(0);
        var whence = context.HasArgument(1)
            ? context.GetArgument<string>(1)
            : "cur";
        var offset = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : 0;

        if (whence is not ("set" or "cur" or "end"))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), $"bad argument #2 to 'seek' (invalid option '{whence}')");
        }

        if (!MathEx.IsInteger(offset))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), $"bad argument #3 to 'seek' (number has no integer representation)");
        }

        try
        {
            buffer.Span[0] = file.Seek(whence, (long)offset);
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