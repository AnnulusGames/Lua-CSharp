using Lua.CodeAnalysis.Compilation;
using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class DoFileFunction : LuaFunction
{
    public override string Name => "dofile";
    public static readonly DoFileFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<string>(0);

        // do not use LuaState.DoFileAsync as it uses the new LuaFunctionExecutionContext
        var text = await File.ReadAllTextAsync(arg0, cancellationToken);
        var fileName = Path.GetFileName(arg0);
        var chunk = LuaCompiler.Default.Compile(text, fileName);

        return await new Closure(context.State, chunk).InvokeAsync(context, buffer, cancellationToken);
    }
}