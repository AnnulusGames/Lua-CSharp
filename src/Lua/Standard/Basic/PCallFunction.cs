using Lua.Internal;

namespace Lua.Standard.Basic;

public sealed class PCallFunction : LuaFunction
{
    public override string Name => "pcall";
    public static readonly PCallFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);

        try
        {
            using var methodBuffer = new PooledArray<LuaValue>(1024);

            var resultCount = await arg0.InvokeAsync(context with
            {
                State = context.State,
                ArgumentCount = context.ArgumentCount - 1,
                StackPosition = context.StackPosition + 1,
            }, methodBuffer.AsMemory(), cancellationToken);

            buffer.Span[0] = true;
            methodBuffer.AsSpan()[..resultCount].CopyTo(buffer.Span[1..]);

            return resultCount + 1;
        }
        catch (Exception ex)
        {
            buffer.Span[0] = false;
            buffer.Span[1] = ex.Message;
            return 2;
        }
    }
}
