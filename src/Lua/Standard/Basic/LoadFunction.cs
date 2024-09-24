using Lua.CodeAnalysis.Compilation;
using Lua.Runtime;

namespace Lua.Standard.Basic;

public sealed class LoadFunction : LuaFunction
{
    public override string Name => "load";
    public static readonly LoadFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        // Lua-CSharp does not support binary chunks, the mode argument is ignored.
        var arg0 = context.ReadArgument(0);

        var arg1 = context.ArgumentCount >= 2
            ? context.ReadArgument<string>(1)
            : null;

        var arg3 = context.ArgumentCount >= 4
            ? context.ReadArgument<LuaTable>(3)
            : null;

        // do not use LuaState.DoFileAsync as it uses the new LuaFunctionExecutionContext
        try
        {
            if (arg0.TryRead<string>(out var str))
            {
                var chunk = LuaCompiler.Default.Compile(str, arg1 ?? "chunk");
                buffer.Span[0] = new Closure(context.State, chunk, arg3);
                return new(1);
            }
            else if (arg0.TryRead<LuaFunction>(out var function))
            {
                // TODO: 
                throw new NotImplementedException();
            }
            else
            {
                LuaRuntimeException.BadArgument(context.State.GetTraceback(), 1, Name);
                return default; // dummy
            }
        }
        catch (Exception ex)
        {
            buffer.Span[0] = LuaValue.Nil;
            buffer.Span[1] = ex.Message;
            return new(2);
        }
    }
}