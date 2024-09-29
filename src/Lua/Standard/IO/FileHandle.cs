using Lua.Runtime;

namespace Lua.Standard.IO;

// TODO: optimize (remove StreamReader/Writer)

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
                    "lines" => FileLinesFunction.Instance,
                    "flush" => FileFlushFunction.Instance,
                    "setvbuf" => FileSetVBufFunction.Instance,
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
    StreamWriter? writer;
    StreamReader? reader;
    bool isClosed;

    public bool IsClosed => Volatile.Read(ref isClosed);

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
        if (stream.CanWrite) writer = new StreamWriter(stream);
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

    public int ReadByte()
    {
        return stream.ReadByte();
    }

    public void Write(ReadOnlySpan<char> buffer)
    {
        writer!.Write(buffer);
    }

    public long Seek(string whence, long offset)
    {
        if (whence != null)
        {
            switch (whence)
            {
                case "set":
                    stream.Seek(offset, SeekOrigin.Begin);
                    break;
                case "cur":
                    stream.Seek(offset, SeekOrigin.Current);
                    break;
                case "end":
                    stream.Seek(offset, SeekOrigin.End);
                    break;
                default:
                    throw new ArgumentException($"Invalid option '{whence}'");
            }
        }

        return stream.Position;
    }

    public void Flush()
    {
        writer!.Flush();
    }

    public void SetVBuf(string mode, int size)
    {
        // Ignore size parameter

        if (writer != null)
        {
            writer.AutoFlush = mode is "no" or "line";
        }
    }

    public void Close()
    {
        if (isClosed) throw new ObjectDisposedException(nameof(FileHandle));
        Volatile.Write(ref isClosed, true);

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