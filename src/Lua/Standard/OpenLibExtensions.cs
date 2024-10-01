using Lua.Standard.Basic;
using Lua.Standard.Coroutines;
using Lua.Standard.IO;
using Lua.Standard.Mathematics;
using Lua.Standard.Modules;
using Lua.Standard.OperatingSystem;
using Lua.Standard.Table;

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
        Basic.TypeFunction.Instance,
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

    static readonly LuaFunction[] tableFunctions = [
        PackFunction.Instance,
        UnpackFunction.Instance,
        Table.RemoveFunction.Instance,
        ConcatFunction.Instance,
        InsertFunction.Instance,
        SortFunction.Instance,
    ];

    static readonly LuaFunction[] ioFunctions = [
        OpenFunction.Instance,
        CloseFunction.Instance,
        InputFunction.Instance,
        OutputFunction.Instance,
        WriteFunction.Instance,
        ReadFunction.Instance,
        LinesFunction.Instance,
        IO.TypeFunction.Instance,
    ];

    static readonly LuaFunction[] osFunctions = [
        ClockFunction.Instance,
        DiffTimeFunction.Instance,
        ExitFunction.Instance,
        GetEnvFunction.Instance,
        OperatingSystem.RemoveFunction.Instance,
        SetLocaleFunction.Instance,
        TimeFunction.Instance,
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
        state.Environment[RandomFunction.RandomInstanceKey] = new LuaUserData<Random>(new Random());

        var math = new LuaTable(0, mathFunctions.Length);
        foreach (var func in mathFunctions)
        {
            math[func.Name] = func;
        }

        math["pi"] = Math.PI;
        math["huge"] = double.PositiveInfinity;

        state.Environment["math"] = math;
    }

    public static void OpenModuleLibrary(this LuaState state)
    {
        var package = new LuaTable(0, 1);
        package["loaded"] = new LuaTable();
        state.Environment["package"] = package;

        state.Environment[RequireFunction.Instance.Name] = RequireFunction.Instance;
    }

    public static void OpenTableLibrary(this LuaState state)
    {
        var table = new LuaTable(0, tableFunctions.Length);
        foreach (var func in tableFunctions)
        {
            table[func.Name] = func;
        }

        state.Environment["table"] = table;
    }

    public static void OpenIOLibrary(this LuaState state)
    {
        var io = new LuaTable(0, ioFunctions.Length);
        foreach (var func in ioFunctions)
        {
            io[func.Name] = func;
        }

        io["stdio"] = new FileHandle(Console.OpenStandardInput());
        io["stdout"] = new FileHandle(Console.OpenStandardOutput());
        io["stderr"] = new FileHandle(Console.OpenStandardError());

        state.Environment["io"] = io;
    }

    public static void OpenOperatingSystemLibrary(this LuaState state)
    {
        var os = new LuaTable(0, osFunctions.Length);
        foreach (var func in osFunctions)
        {
            os[func.Name] = func;
        }

        state.Environment["os"] = os;
    }
}