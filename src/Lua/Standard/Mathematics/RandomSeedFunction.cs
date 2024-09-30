
namespace Lua.Standard.Mathematics;

public sealed class RandomSeedFunction : LuaFunction
{
    public static readonly RandomSeedFunction Instance = new();

    public override string Name => "randomseed";

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        context.State.Environment[RandomFunction.RandomInstanceKey] = new LuaUserData<Random>(new Random((int)BitConverter.DoubleToInt64Bits(arg0)));
        return new(0);
    }
}