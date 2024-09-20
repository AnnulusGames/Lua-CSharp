namespace Lua;

partial class LuaFunction
{
    sealed class AnonymousLuaFunction(Func<LuaValue[], CancellationToken, ValueTask<LuaValue[]>> func) : LuaFunction
    {
        protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var args = context.ArgumentCount == 0 ? [] : new LuaValue[context.ArgumentCount];
            context.Arguments.CopyTo(args);

            var result = await func(args, cancellationToken);
            if (result != null)
            {
                result.AsMemory().CopyTo(buffer);
                return result.Length;
            }
            else
            {
                return 0;
            }
        }
    }

    public static LuaFunction Create(Func<LuaValue[], CancellationToken, ValueTask<LuaValue[]>> func)
    {
        return new AnonymousLuaFunction(func);
    }
}