using System.Buffers.Text;
using System.Text;
using Lua.Internal;

namespace Lua.Standard.IO;

public sealed class FileReadFunction : LuaFunction
{
    public override string Name => "read";
    public static readonly FileReadFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var file = context.ReadArgument<FileHandle>(0);
        var resultCount = IOHelper.Read(file, Name, 1, false, context, context.Arguments[1..], buffer);
        return new(resultCount);
    }
}