using System.Buffers.Text;
using System.Text;
using Lua.Internal;

namespace Lua.Standard.IO;

internal static class IOHelper
{
    public static int Open(LuaState state, string fileName, string mode, Memory<LuaValue> buffer, bool throwError)
    {
        var fileMode = mode switch
        {
            "r" or "rb" or "r+" or "r+b" => FileMode.Open,
            "w" or "wb" or "w+" or "w+b" => FileMode.Create,
            "a" or "ab" or "a+" or "a+b" => FileMode.Append,
            _ => throw new LuaRuntimeException(state.GetTraceback(), "bad argument #2 to 'open' (invalid mode)"),
        };

        var fileAccess = mode switch
        {
            "r" or "rb" => FileAccess.Read,
            "w" or "wb" or "a" or "ab" => FileAccess.Write,
            _ => FileAccess.ReadWrite,
        };

        try
        {
            var stream = File.Open(fileName, fileMode, fileAccess);
            buffer.Span[0] = new FileHandle(stream);
            return 1;
        }
        catch (IOException ex)
        {
            if (throwError)
            {
                throw;
            }

            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return 3;
        }
    }

    // TODO: optimize (use IBuffertWrite<byte>, async)

    public static int Write(FileHandle file, string name, LuaFunctionExecutionContext context, Memory<LuaValue> buffer)
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
                    file.Write(fileBuffer.AsSpan()[..bytesWritten]);
                }
                else if (arg.TryRead<double>(out var d))
                {
                    using var fileBuffer = new PooledArray<byte>(64);
                    if (!Utf8Formatter.TryFormat(d, fileBuffer.AsSpan(), out var bytesWritten))
                    {
                        throw new ArgumentException("Destination is too short.");
                    }
                    file.Write(fileBuffer.AsSpan()[..bytesWritten]);
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

    public static int Read(LuaState state, FileHandle file, string name, int startArgumentIndex, ReadOnlySpan<LuaValue> formats, Memory<LuaValue> buffer, bool throwError)
    {
        if (formats.Length == 0)
        {
            formats = defaultReadFormat;
        }

        try
        {
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
                            buffer.Span[i] = file.ReadToEnd();
                            break;
                        case "*l":
                        case "*line":
                            buffer.Span[i] = file.ReadLine() ?? LuaValue.Nil;
                            break;
                        case "L":
                        case "*L":
                            var text = file.ReadLine();
                            buffer.Span[i] = text == null ? LuaValue.Nil : text + Environment.NewLine;
                            break;
                    }
                }
                else if (format.TryRead<double>(out var d))
                {
                    if (!MathEx.IsInteger(d))
                    {
                        throw new LuaRuntimeException(state.GetTraceback(), $"bad argument #{i + startArgumentIndex} to 'read' (number has no integer representation)");
                    }

                    var count = (int)d;
                    using var byteBuffer = new PooledArray<byte>(count);

                    for (int j = 0; j < count; j++)
                    {
                        var b = file.ReadByte();
                        if (b == -1)
                        {
                            buffer.Span[0] = LuaValue.Nil;
                            return 1;
                        }

                        byteBuffer[j] = (byte)b;
                    }

                    buffer.Span[i] = Encoding.UTF8.GetString(byteBuffer.AsSpan());
                }
                else
                {
                    LuaRuntimeException.BadArgument(state.GetTraceback(), i + 1, name);
                }
            }

            return formats.Length;
        }
        catch (IOException ex)
        {
            if (throwError)
            {
                throw;
            }

            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return 3;
        }
    }
}