using Lua.Standard.Base;
using Lua.Standard.Coroutines;
using Lua.Standard.Mathematics;

namespace Lua.Standard;

public static class OpenLibExtensions
{
    static readonly LuaFunction[] baseFunctions = [
        AssertFunction.Instance,
        ErrorFunction.Instance,
        PrintFunction.Instance,
        RawGetFunction.Instance,
        RawSetFunction.Instance,
        GetMetatableFunction.Instance,
        SetMetatableFunction.Instance,
        ToStringFunction.Instance
    ];

    static readonly LuaFunction[] mathFunctions = [
        AbsFunction.Instance,
        AcosFunction.Instance,
        AsinFunction.Instance,
        Atan2Function.Instance,
        AtanFunction.Instance,
        CeilFunction.Instance,
        CosFunction.Instance,
        CoshFunction.Instance,
        DegFunction.Instance,
        ExpFunction.Instance,
        FloorFunction.Instance,
        FmodFunction.Instance,
        FrexpFunction.Instance,
        LdexpFunction.Instance,
        LogFunction.Instance,
        MaxFunction.Instance,
        MinFunction.Instance,
        ModfFunction.Instance,
        PowFunction.Instance,
        RadFunction.Instance,
        RandomFunction.Instance,
        RandomSeedFunction.Instance,
        SinFunction.Instance,
        SinhFunction.Instance,
        SqrtFunction.Instance,
        TanFunction.Instance,
        TanhFunction.Instance,
    ];

    public static void OpenBaseLibrary(this LuaState state)
    {
        state.Environment["_G"] = state.Environment;
        state.Environment["_VERSION"] = "Lua 5.2";
        foreach (var func in baseFunctions)
        {
            state.Environment[func.Name] = func;
        }
    }

    public static void OpenMathLibrary(this LuaState state)
    {
        state.Environment[RandomFunction.RandomInstanceKey] = new(new Random());
        state.Environment["pi"] = Math.PI;
        state.Environment["huge"] = double.PositiveInfinity;

        var table = new LuaTable(0, mathFunctions.Length);
        foreach (var func in mathFunctions)
        {
            table[func.Name] = func;
        }

        state.Environment["math"] = table;
    }
    
    public static void OpenCoroutineLibrary(this LuaState state)
    {
        var table = new LuaTable(0, 6);
        table[CoroutineCreateFunction.FunctionName] = new CoroutineCreateFunction();
        table[CoroutineResumeFunction.FunctionName] = new CoroutineResumeFunction();
        table[CoroutineYieldFunction.FunctionName] = new CoroutineYieldFunction();
        table[CoroutineStatusFunction.FunctionName] = new CoroutineStatusFunction();
        table[CoroutineRunningFunction.FunctionName] = new CoroutineRunningFunction();

        state.Environment["coroutine"] = table;
    }
}