using Lua.Runtime;

namespace Lua.Standard.IO;

public class FileHandle : LuaUserData
{
    class IndexMetamethod : LuaFunction
    {
        protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            context.ReadArgument<FileHandle>(0);
            var key = context.ReadArgument(1);

            if (key.TryRead<string>(out var name))
            {
                buffer.Span[0] = name switch
                {
                    "write" => FileWriteFunction.Instance,
                    "read" => FileReadFunction.Instance,
                    "flush" => FileFlushFunction.Instance,
                    "close" => CloseFunction.Instance,
                    _ => LuaValue.Nil,
                };
            }
            else
            {
                buffer.Span[0] = LuaValue.Nil;
            }

            return new(1);
        }
    }

    public Stream Stream { get; }
    public StreamReader? Reader { get; }

    static readonly LuaTable fileHandleMetatable;

    static FileHandle()
    {
        fileHandleMetatable = new LuaTable();
        fileHandleMetatable[Metamethods.Index] = new IndexMetamethod();
    }

    public FileHandle(Stream stream)
    {
        Stream = stream;
        if (stream.CanRead) Reader = new StreamReader(stream);
        Metatable = fileHandleMetatable;
    }
}