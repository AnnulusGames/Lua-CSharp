namespace Lua.Standard.IO;

public sealed class FileSetVBufFunction : LuaFunction
{
    public override string Name => "setvbuf";
    public static readonly FileSetVBufFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.ReadArgument<FileHandle>(0);
        var mode = context.ReadArgument<string>(1);
        var size = context.ArgumentCount >= 3
            ? context.ReadArgument<double>(2)
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