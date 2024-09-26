namespace Lua.Standard.IO;

public sealed class OutputFunction : LuaFunction
{
    public override string Name => "output";
    public static readonly OutputFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var io = context.State.Environment["io"].Read<LuaTable>();

        if (context.ArgumentCount == 0 || context.Arguments[0].Type is LuaValueType.Nil)
        {
            buffer.Span[0] = io["stdout"];
            return new(1);
        }

        var arg = context.Arguments[0];
        if (arg.TryRead<FileHandle>(out var file))
        {
            io["stdout"] = file;
            buffer.Span[0] = file;
            return new(1);
        }
        else
        {
            var stream = File.Open(arg.ToString()!, FileMode.Open, FileAccess.ReadWrite);
            var handle = new FileHandle(stream);
            io["stdout"] = handle;
            buffer.Span[0] = handle;
            return new(1);
        }
    }
}