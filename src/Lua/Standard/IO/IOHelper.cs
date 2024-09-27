using System.Buffers.Text;
using System.Text;
using Lua.Internal;

namespace Lua.Standard.IO;

internal static class IOHelper
{
    // TODO: optimize (use IBuffertWrite<byte>, async)

    public static int Write(FileHandle file, string name, LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
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
                    LuaRuntimeException.BadArgument(context.State.GetTraceback(), i + 1, name);
                }
            }
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return 3;
        }

        buffer.Span[0] = file;
        return 1;
    }

    static readonly LuaValue[] defaultReadFormat = ["*l"];

    public static int Read(FileHandle file, string name, LuaFunctionExecutionContext context, ReadOnlySpan<LuaValue> formats, Memory<LuaValue> buffer)
    {
        if (formats.Length == 0)
        {
            formats = defaultReadFormat;
        }

        try
        {
            var reader = file.Reader!;

            for (int i = 0; i < formats.Length; i++)
            {
                var format = formats[i];
                if (format.TryRead<string>(out var str))
                {
                    switch (str)
                    {
                        case "*n":
                        case "*number":
                            // TODO: support number format
                            throw new NotImplementedException();
                        case "*a":
                        case "*all":
                            buffer.Span[i] = reader.ReadToEnd();
                            break;
                        case "*l":
                        case "*line":
                            buffer.Span[i] = reader.ReadLine() ?? LuaValue.Nil;
                            break;
                        case "L":
                        case "*L":
                            var text = reader.ReadLine();
                            buffer.Span[i] = text == null ? LuaValue.Nil : text + Environment.NewLine;
                            break;
                    }
                }
                else if (format.TryRead<double>(out var d))
                {
                    if (!MathEx.IsInteger(d))
                    {
                        throw new LuaRuntimeException(context.State.GetTraceback(), $"bad argument #{i + 1} to 'read' (number has no integer representation)");
                    }
                    
                    // TODO:

                }
                else
                {
                    LuaRuntimeException.BadArgument(context.State.GetTraceback(), i + 1, name);
                }
            }

            return formats.Length;
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return 3;
        }
    }
}