using System.Buffers.Text;
using System.Text;
using Lua.Internal;

namespace Lua.Standard.IO;

// TODO: optimize (use IBuffertWrite<byte>)

public sealed class FileWriteFunction : LuaFunction
{
    public override string Name => "write";
    public static readonly FileWriteFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.ReadArgument<FileHandle>(0);
        try
        {
            for (int i = 1; i < context.ArgumentCount; i++)
            {
                var arg = context.Arguments[i];
                if (arg.TryRead<string>(out var str))
                {
                    using var fileBuffer = new PooledArray<byte>(str.Length * 3);
                    var bytesWritten = Encoding.UTF8.GetBytes(str, fileBuffer.AsSpan());
                    file.Stream.Write(fileBuffer.AsSpan()[..bytesWritten]);
                }
                else if (arg.TryRead<double>(out var d))
                {
                    using var fileBuffer = new PooledArray<byte>(64);
                    if (!Utf8Formatter.TryFormat(d, fileBuffer.AsSpan(), out var bytesWritten))
                    {
                        throw new ArgumentException("Destination is too short.");
                    }
                    file.Stream.Write(fileBuffer.AsSpan()[..bytesWritten]);
                }
                else
                {
                    LuaRuntimeException.BadArgument(context.State.GetTraceback(), i + 1, Name);
                }
            }
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }

        buffer.Span[0] = file;
        return new(1);
    }
}