
using Lua.Runtime;

namespace Lua.Standard.Coroutines;

public sealed class CoroutineWrapFunction : LuaFunction
{
    public static readonly CoroutineWrapFunction Instance = new();
    public override string Name => "wrap";


    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaFunction>(0);
        var thread = new LuaCoroutine(arg0, false);
        buffer.Span[0] = new Wrapper(thread);
        return new(1);
    }

    class Wrapper(LuaThread targetThread) : LuaFunction
    {
        protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var stack = context.Thread.Stack;
            var frameBase = stack.Count;

            stack.Push(targetThread);
            PushArguments(stack, context.Arguments);

            var resultCount = await targetThread.Resume(context with
            {
                ArgumentCount = context.ArgumentCount + 1,
                FrameBase = frameBase,
            }, buffer, cancellationToken);

            buffer.Span[1..].CopyTo(buffer.Span[0..]);
            return resultCount - 1;
        }

        static void PushArguments(LuaStack stack, ReadOnlySpan<LuaValue> arguments)
        {
            foreach (var arg in arguments)
            {
                stack.Push(arg);
            }
        }
    }
}