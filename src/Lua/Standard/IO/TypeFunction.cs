namespace Lua.Standard.IO;

public sealed class TypeFunction : LuaFunction
{
    public override string Name => "type";
    public static readonly TypeFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument(0);

        if (arg0.TryRead<FileHandle>(out var file))
        {
            buffer.Span[0] = file.IsClosed ? "closed file" : "file";
        }
        else
        {
            buffer.Span[0] = LuaValue.Nil;
        }

        return new(1);
    }
}