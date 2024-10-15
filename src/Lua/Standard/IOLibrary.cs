using Lua.Internal;
using Lua.Standard.Internal;

namespace Lua.Standard;

public static class IOLibrary
{
    public static void OpenIOLibrary(this LuaState state)
    {
        var io = new LuaTable(0, Functions.Length);
        foreach (var func in Functions)
        {
            io[func.Name] = func;
        }

        io["stdio"] = new FileHandle(Console.OpenStandardInput()).AsLuaValue();
        io["stdout"] = new FileHandle(Console.OpenStandardOutput()).AsLuaValue();
        io["stderr"] = new FileHandle(Console.OpenStandardError()).AsLuaValue();

        state.Environment["io"] = io;
        state.LoadedModules["io"] = io;
    }

    static readonly LuaFunction[] Functions = [
        new("close", Close),
        new("flush", Flush),
        new("input", Input),
        new("lines", Lines),
        new("open", Open),
        new("output", Output),
        new("read", Read),
        new("type", Type),
        new("write", Write),
    ];

    public static ValueTask<int> Close(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.HasArgument(0)
            ? context.GetArgument<FileHandle>(0)
            : context.State.Environment["io"].Read<LuaTable>()["stdout"].Read<FileHandle>();

        try
        {
            file.Close();
            buffer.Span[0] = true;
            return new(1);
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }
    }

    public static ValueTask<int> Flush(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.State.Environment["io"].Read<LuaTable>()["stdout"].Read<FileHandle>();

        try
        {
            file.Flush();
            buffer.Span[0] = true;
            return new(1);
        }
        catch (IOException ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            buffer.Span[2] = ex.HResult;
            return new(3);
        }
    }

    public static ValueTask<int> Input(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var io = context.State.Environment["io"].Read<LuaTable>();

        if (context.ArgumentCount == 0 || context.Arguments[0].Type is LuaValueType.Nil)
        {
            buffer.Span[0] = io["stdio"];
            return new(1);
        }

        var arg = context.Arguments[0];
        if (arg.TryRead<FileHandle>(out var file))
        {
            io["stdio"] = file.AsLuaValue();
            buffer.Span[0] = file.AsLuaValue();
            return new(1);
        }
        else
        {
            var stream = File.Open(arg.ToString()!, FileMode.Open, FileAccess.ReadWrite);
            var handle = new FileHandle(stream);
            io["stdio"] = handle.AsLuaValue();
            buffer.Span[0] = handle.AsLuaValue();
            return new(1);
        }
    }

    public static ValueTask<int> Lines(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (context.ArgumentCount == 0)
        {
            var file = context.State.Environment["io"].Read<LuaTable>()["stdio"].Read<FileHandle>();
            buffer.Span[0] = new LuaFunction("iterator", (context, buffer, ct) =>
            {
                var resultCount = IOHelper.Read(context.State, file, "lines", 0, [], buffer, true);
                if (resultCount > 0 && buffer.Span[0].Type is LuaValueType.Nil)
                {
                    file.Close();
                }
                return new(resultCount);
            });
            return new(1);
        }
        else
        {
            var fileName = context.GetArgument<string>(0);

            using var methodBuffer = new PooledArray<LuaValue>(32);
            IOHelper.Open(context.State, fileName, "r", methodBuffer.AsMemory(), true);

            var file = methodBuffer[0].Read<FileHandle>();
            var formats = context.Arguments[1..].ToArray();

            buffer.Span[0] = new LuaFunction("iterator", (context, buffer, ct) =>
            {
                var resultCount = IOHelper.Read(context.State, file, "lines", 0, formats, buffer, true);
                if (resultCount > 0 && buffer.Span[0].Type is LuaValueType.Nil)
                {
                    file.Close();
                }
                return new(resultCount);
            });

            return new(1);
        }
    }

    public static ValueTask<int> Open(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var fileName = context.GetArgument<string>(0);
        var mode = context.HasArgument(1)
            ? context.GetArgument<string>(1)
            : "r";

        var resultCount = IOHelper.Open(context.State, fileName, mode, buffer, false);
        return new(resultCount);
    }

    public static ValueTask<int> Output(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
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
            io["stdout"] = file.AsLuaValue();
            buffer.Span[0] = file.AsLuaValue();
            return new(1);
        }
        else
        {
            var stream = File.Open(arg.ToString()!, FileMode.Open, FileAccess.ReadWrite);
            var handle = new FileHandle(stream);
            io["stdout"] = handle.AsLuaValue();
            buffer.Span[0] = handle.AsLuaValue();
            return new(1);
        }
    }

    public static ValueTask<int> Read(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.State.Environment["io"].Read<LuaTable>()["stdio"].Read<FileHandle>();
        var resultCount = IOHelper.Read(context.State, file, "read", 0, context.Arguments, buffer, false);
        return new(resultCount);
    }

    public static ValueTask<int> Type(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
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

    public static ValueTask<int> Write(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.State.Environment["io"].Read<LuaTable>()["stdout"].Read<FileHandle>();
        var resultCount = IOHelper.Write(file, "write", context, buffer);
        return new(resultCount);
    }
}