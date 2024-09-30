namespace Lua.Standard.IO;

public sealed class FileLinesFunction : LuaFunction
{
    public override string Name => "lines";
    public static readonly FileLinesFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<FileHandle>(0);
        var arg1 = context.HasArgument(1)
            ? context.Arguments[1]
            : "*l";

        buffer.Span[0] = new Iterator(arg0, arg1);
        return new(1);
    }

    class Iterator(FileHandle file, LuaValue format) : LuaFunction
    {
        readonly LuaValue[] formats = [format];

        protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var resultCount = IOHelper.Read(context.State, file, Name, 0, formats, buffer, true);
            return new(resultCount);
        }
    }
}