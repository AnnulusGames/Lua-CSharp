
namespace Lua.Standard.Mathematics;

public sealed class RandomFunction : LuaFunction
{
    public const string RandomInstanceKey = "__lua_mathematics_library_random_instance";
    public static readonly RandomFunction Instance = new();

    public override string Name => "random";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var rand = context.State.Environment[RandomInstanceKey].Read<LuaUserData<Random>>().Value;

        if (context.ArgumentCount == 0)
        {
            buffer.Span[0] = rand.NextDouble();
        }
        else if (context.ArgumentCount == 1)
        {
            var arg0 = context.GetArgument<double>(0);
            buffer.Span[0] = rand.NextDouble() * (arg0 - 1) + 1;
        }
        else
        {
            var arg0 = context.GetArgument<double>(0);
            var arg1 = context.GetArgument<double>(1);
            buffer.Span[0] = rand.NextDouble() * (arg1 - arg0) + arg0;
        }

        return new(1);
    }
}