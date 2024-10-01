using Lua.Internal;

namespace Lua.Standard.Basic;

public sealed class XPCallFunction : LuaFunction
{
    public override string Name => "xpcall";
    public static readonly XPCallFunction Instance = new();

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);
        var arg1 = context.GetArgument<LuaFunction>(1);

        using var methodBuffer = new PooledArray<LuaValue>(1024);
        methodBuffer.AsSpan().Clear();

        try
        {
            var resultCount = await arg0.InvokeAsync(context with
            {
                State = context.State,
                ArgumentCount = context.ArgumentCount - 2,
                StackPosition = context.StackPosition + 2,
            }, methodBuffer.AsMemory(), cancellationToken);

            buffer.Span[0] = true;
            methodBuffer.AsSpan()[..resultCount].CopyTo(buffer.Span[1..]);

            return resultCount + 1;
        }
        catch (Exception ex)
        {
            methodBuffer.AsSpan().Clear();

            context.State.Push(ex.Message);

            // invoke error handler
            await arg1.InvokeAsync(context with
            {
                State = context.State,
                ArgumentCount = 1,
                StackPosition = null,
            }, methodBuffer.AsMemory(), cancellationToken);

            buffer.Span[0] = false;
            buffer.Span[1] = ex.Message;

            return 2;
        }
    }
}
