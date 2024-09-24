using Lua.Standard.Basic;
using Lua.Standard.Coroutines;
using Lua.Standard.Mathematics;
using Lua.Standard.Modules;

namespace Lua.Standard;

public static class OpenLibExtensions
{
    static readonly LuaFunction[] baseFunctions = [
        AssertFunction.Instance,
        ErrorFunction.Instance,
        PrintFunction.Instance,
        RawGetFunction.Instance,
        RawSetFunction.Instance,
        RawEqualFunction.Instance,
        RawLenFunction.Instance,
        GetMetatableFunction.Instance,
        SetMetatableFunction.Instance,
        ToNumberFunction.Instance,
        ToStringFunction.Instance,
        CollectGarbageFunction.Instance,
        NextFunction.Instance,
        IPairsFunction.Instance,
        PairsFunction.Instance,
        TypeFunction.Instance,
        PCallFunction.Instance,
        XPCallFunction.Instance,
        DoFileFunction.Instance,
        LoadFileFunction.Instance,
        LoadFunction.Instance,
        SelectFunction.Instance,
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

    public static void OpenBasicLibrary(this LuaState state)
    {
        // basic
        state.Environment["_G"] = state.Environment;
        state.Environment["_VERSION"] = "Lua 5.2";
        foreach (var func in baseFunctions)
        {
            state.Environment[func.Name] = func;
        }

        // coroutine
        var coroutine = new LuaTable(0, 6);
        coroutine[CoroutineCreateFunction.FunctionName] = new CoroutineCreateFunction();
        coroutine[CoroutineResumeFunction.FunctionName] = new CoroutineResumeFunction();
        coroutine[CoroutineYieldFunction.FunctionName] = new CoroutineYieldFunction();
        coroutine[CoroutineStatusFunction.FunctionName] = new CoroutineStatusFunction();
        coroutine[CoroutineRunningFunction.FunctionName] = new CoroutineRunningFunction();
        coroutine[CoroutineWrapFunction.FunctionName] = new CoroutineWrapFunction();

        state.Environment["coroutine"] = coroutine;
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

    public static void OpenModuleLibrary(this LuaState state)
    {
        var package = new LuaTable(0, 1);
        package["loaded"] = new LuaTable();
        state.Environment["package"] = package;
        
        state.Environment[RequireFunction.Instance.Name] = RequireFunction.Instance;
    }
}