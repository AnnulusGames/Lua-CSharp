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

    Stream stream;
    StreamReader? reader;

    static readonly LuaTable fileHandleMetatable;

    static FileHandle()
    {
        fileHandleMetatable = new LuaTable();
        fileHandleMetatable[Metamethods.Index] = new IndexMetamethod();
    }

    public FileHandle(Stream stream)
    {
        this.stream = stream;
        if (stream.CanRead) reader = new StreamReader(stream);
        Metatable = fileHandleMetatable;
    }

    public string? ReadLine()
    {
        return reader!.ReadLine();
    }

    public string ReadToEnd()
    {
        return reader!.ReadToEnd();
    }

    public void Write(ReadOnlySpan<byte> buffer)
    {
        stream.Write(buffer);
    }

    public void Flush()
    {
        stream.Flush();
    }

    public void Close()
    {
        if (reader != null)
        {
            reader.Dispose();
        }
        else
        {
            stream.Close();
        }
    }
}