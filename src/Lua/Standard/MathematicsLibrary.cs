namespace Lua.Standard;

public static class MathematicsLibrary
{
    public static void OpenMathLibrary(this LuaState state)
    {
        state.Environment[RandomInstanceKey] = new LuaUserData<Random>(new Random());

        var math = new LuaTable(0, Functions.Length);
        foreach (var func in Functions)
        {
            math[func.Name] = func;
        }

        math["pi"] = Math.PI;
        math["huge"] = double.PositiveInfinity;

        state.Environment["math"] = math;
        state.LoadedModules["math"] = math;
    }

    public const string RandomInstanceKey = "__lua_mathematics_library_random_instance";

    static readonly LuaFunction[] Functions = [
        new("abs", Abs),
        new("acos", Acos),
        new("asin", Asin),
        new("atan2", Atan2),
        new("atan", Atan),
        new("ceil", Ceil),
        new("cos", Cos),
        new("cosh", Cosh),
        new("deg", Deg),
        new("exp", Exp),
        new("floor", Floor),
        new("fmod", Fmod),
        new("frexp", Frexp),
        new("ldexp", Ldexp),
        new("log", Log),
        new("max", Max),
        new("min", Min),
        new("modf", Modf),
        new("pow", Pow),
        new("rad", Rad),
        new("random", Random),
        new("randomseed", RandomSeed),
        new("sin", Sin),
        new("sinh", Sinh),
        new("sqrt", Sqrt),
        new("tan", Tan),
        new("tanh", Tanh),
    ];

    public static ValueTask<int> Abs(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Abs(arg0);
        return new(1);
    }

    public static ValueTask<int> Acos(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Acos(arg0);
        return new(1);
    }

    public static ValueTask<int> Asin(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Asin(arg0);
        return new(1);
    }

    public static ValueTask<int> Atan2(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);

        buffer.Span[0] = Math.Atan2(arg0, arg1);
        return new(1);
    }

    public static ValueTask<int> Atan(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Atan(arg0);
        return new(1);
    }

    public static ValueTask<int> Ceil(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Ceiling(arg0);
        return new(1);
    }

    public static ValueTask<int> Cos(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Cos(arg0);
        return new(1);
    }

    public static ValueTask<int> Cosh(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Cosh(arg0);
        return new(1);
    }

    public static ValueTask<int> Deg(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = arg0 * (180.0 / Math.PI);
        return new(1);
    }

    public static ValueTask<int> Exp(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Exp(arg0);
        return new(1);
    }

    public static ValueTask<int> Floor(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Floor(arg0);
        return new(1);
    }

    public static ValueTask<int> Fmod(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);
        buffer.Span[0] = arg0 % arg1;
        return new(1);
    }

    public static ValueTask<int> Frexp(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);

        var (m, e) = MathEx.Frexp(arg0);
        buffer.Span[0] = m;
        buffer.Span[1] = e;
        return new(2);
    }

    public static ValueTask<int> Ldexp(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);

        buffer.Span[0] = arg0 * Math.Pow(2, arg1);
        return new(1);
    }

    public static ValueTask<int> Log(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);

        if (context.ArgumentCount == 1)
        {
            buffer.Span[0] = Math.Log(arg0);
        }
        else
        {
            var arg1 = context.GetArgument<double>(1);
            buffer.Span[0] = Math.Log(arg0, arg1);
        }

        return new(1);
    }

    public static ValueTask<int> Max(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.GetArgument<double>(0);
        for (int i = 1; i < context.ArgumentCount; i++)
        {
            x = Math.Max(x, context.GetArgument<double>(i));
        }

        buffer.Span[0] = x;

        return new(1);
    }

    public static ValueTask<int> Min(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var x = context.GetArgument<double>(0);
        for (int i = 1; i < context.ArgumentCount; i++)
        {
            x = Math.Min(x, context.GetArgument<double>(i));
        }

        buffer.Span[0] = x;

        return new(1);
    }

    public static ValueTask<int> Modf(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var (i, f) = MathEx.Modf(arg0);
        buffer.Span[0] = i;
        buffer.Span[1] = f;
        return new(2);
    }

    public static ValueTask<int> Pow(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        var arg1 = context.GetArgument<double>(1);

        buffer.Span[0] = Math.Pow(arg0, arg1);
        return new(1);
    }

    public static ValueTask<int> Rad(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = arg0 * (Math.PI / 180.0);
        return new(1);
    }

    public static ValueTask<int> Random(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
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

    public static ValueTask<int> RandomSeed(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        context.State.Environment[RandomInstanceKey] = new LuaUserData<Random>(new Random((int)BitConverter.DoubleToInt64Bits(arg0)));
        return new(0);
    }

    public static ValueTask<int> Sin(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Sin(arg0);
        return new(1);
    }

    public static ValueTask<int> Sinh(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Sinh(arg0);
        return new(1);
    }

    public static ValueTask<int> Sqrt(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Sqrt(arg0);
        return new(1);
    }

    public static ValueTask<int> Tan(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Tan(arg0);
        return new(1);
    }

    public static ValueTask<int> Tanh(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<double>(0);
        buffer.Span[0] = Math.Tanh(arg0);
        return new(1);
    }
}