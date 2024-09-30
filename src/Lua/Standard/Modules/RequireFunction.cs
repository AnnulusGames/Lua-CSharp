
using Lua.CodeAnalysis.Compilation;
using Lua.Internal;
using Lua.Runtime;

namespace Lua.Standard.Modules;

public sealed class RequireFunction : LuaFunction
{
    public override string Name => "require";
    public static readonly RequireFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<string>(0);
        var loaded = context.State.Environment["package"].Read<LuaTable>()["loaded"].Read<LuaTable>();

        if (!loaded.TryGetValue(arg0, out var loadedTable))
        {
            var module = await context.State.ModuleLoader.LoadAsync(arg0, cancellationToken);
            var chunk = LuaCompiler.Default.Compile(module.ReadText(), module.Name);

            using var methodBuffer = new PooledArray<LuaValue>(1);
            await new Closure(context.State, chunk).InvokeAsync(context, methodBuffer.AsMemory(), cancellationToken);

            loadedTable = methodBuffer[0];
            loaded[arg0] = loadedTable;
        }

        buffer.Span[0] = loadedTable;
        return 1;
    }
}