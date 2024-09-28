using Lua.Internal;

namespace Lua.Standard.IO;

public sealed class LinesFunction : LuaFunction
{
    public override string Name => "lines";
    public static readonly LinesFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (context.ArgumentCount == 0)
        {
            var file = context.State.Environment["io"].Read<LuaTable>()["stdio"].Read<FileHandle>();
            buffer.Span[0] = new Iterator(file, []);
            return new(1);
        }
        else
        {
            var fileName = context.ReadArgument<string>(0);

            using var methodBuffer = new PooledArray<LuaValue>(32);
            IOHelper.Open(context.State, fileName, "r", methodBuffer.AsMemory(), true);

            var file = methodBuffer[0].Read<FileHandle>();
            buffer.Span[0] = new Iterator(file, context.Arguments[1..]);
            return new(1);
        }
    }

    class Iterator(FileHandle file, ReadOnlySpan<LuaValue> formats) : LuaFunction
    {
        readonly LuaValue[] formats = formats.ToArray();

        protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var resultCount = IOHelper.Read(context.State, file, Name, 0, formats, buffer, true);
            if (resultCount > 0 && buffer.Span[0].Type is LuaValueType.Nil)
            {
                file.Close();
            }
            return new(resultCount);
        }
    }
}