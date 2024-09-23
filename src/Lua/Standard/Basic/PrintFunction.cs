namespace Lua.Standard.Basic;

public sealed class PrintFunction : LuaFunction
{
    public override string Name => "print";
    public static readonly PrintFunction Instance = new();

    LuaValue[] buffer = new LuaValue[1];

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        for (int i = 0; i < context.ArgumentCount; i++)
        {
            await context.Arguments[i].CallToStringAsync(context, this.buffer, cancellationToken);
            Console.Write(this.buffer[0]);
            Console.Write('\t');
        }

        Console.WriteLine();
        return 0;
    }
}