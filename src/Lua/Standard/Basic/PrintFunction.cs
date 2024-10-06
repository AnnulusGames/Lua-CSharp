using Lua.Internal;

namespace Lua.Standard.Basic;

public sealed class PrintFunction : LuaFunction
{
    public override string Name => "print";
    public static readonly PrintFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        using var methodBuffer = new PooledArray<LuaValue>(1);

        for (int i = 0; i < context.ArgumentCount; i++)
        {
            await context.Arguments[i].CallToStringAsync(context, methodBuffer.AsMemory(), cancellationToken);
            Console.Write(methodBuffer[0]);
            Console.Write('\t');
        }

        Console.WriteLine();
        return 0;
    }
}