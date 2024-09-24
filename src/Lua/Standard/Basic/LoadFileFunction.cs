using Lua.CodeAnalysis.Compilation;
using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class LoadFileFunction : LuaFunction
{
    public override string Name => "loadfile";
    public static readonly LoadFileFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        // Lua-CSharp does not support binary chunks, the mode argument is ignored.
        var arg0 = context.ReadArgument<string>(0);
        var arg2 = context.ArgumentCount >= 3
            ? context.ReadArgument<LuaTable>(2)
            : null;

        // do not use LuaState.DoFileAsync as it uses the new LuaFunctionExecutionContext
        try
        {
            var text = await File.ReadAllTextAsync(arg0, cancellationToken);
            var fileName = Path.GetFileName(arg0);
            var chunk = LuaCompiler.Default.Compile(text, fileName);
            buffer.Span[0] = new Closure(context.State, chunk, arg2);
            return 1;
        }
        catch (Exception ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            return 2;
        }
    }
}