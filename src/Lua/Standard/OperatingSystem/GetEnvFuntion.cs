
namespace Lua.Standard.OperatingSystem;

public sealed class GetEnvFunction : LuaFunction
{
    public override string Name => "getenv";
    public static readonly GetEnvFunction Instance = new();

    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var variable = context.GetArgument<string>(0);
        buffer.Span[0] = Environment.GetEnvironmentVariable(variable) ?? LuaValue.Nil;
        return new(1);
    }
}