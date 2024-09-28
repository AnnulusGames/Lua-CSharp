namespace Lua.Standard.IO;

public sealed class WriteFunction : LuaFunction
{
    public override string Name => "write";
    public static readonly WriteFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.State.Environment["io"].Read<LuaTable>()["stdio"].Read<FileHandle>();
        var resultCount = IOHelper.Write(file, Name, context, buffer);
        return new(resultCount);
    }
}