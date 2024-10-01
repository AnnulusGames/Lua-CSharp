namespace Lua.Standard.IO;

public sealed class FileSetVBufFunction : LuaFunction
{
    public override string Name => "setvbuf";
    public static readonly FileSetVBufFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.GetArgument<FileHandle>(0);
        var mode = context.GetArgument<string>(1);
        var size = context.HasArgument(2)
            ? context.GetArgument<double>(2)
            : -1;

        if (!MathEx.IsInteger(size))
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), $"bad argument #3 to 'setvbuf' (number has no integer representation)");
        }

        file.SetVBuf(mode, (int)size);

        buffer.Span[0] = true;
        return new(1);
    }
}