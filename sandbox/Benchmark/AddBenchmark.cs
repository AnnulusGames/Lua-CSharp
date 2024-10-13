using System.Reflection;
using BenchmarkDotNet.Attributes;
using Lua;
using Lua.Standard;
using MoonSharp.Interpreter;

sealed class AddFunction : Lua.LuaFunction
{
    protected override ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        buffer.Span[0] = context.GetArgument<double>(0) + context.GetArgument<double>(1);
        return new(1);
    }
}

[Config(typeof(BenchmarkConfig))]
public class AddBenchmark
{
    BenchmarkCore core = new();
    LuaValue[] buffer = new LuaValue[1];

    public static double Add(double x, double y)
    {
        return x + y;
    }

    [IterationSetup]
    public void Setup()
    {
        core = new();
        core.Setup("add.lua");
        core.LuaCSharpState.OpenStandardLibraries();

        core.LuaCSharpState.Environment["add"] = new AddFunction();
        core.MoonSharpState.Globals["add"] = (Func<double, double, double>)Add;
        core.NLuaState.RegisterFunction("add", typeof(AddBenchmark).GetMethod(nameof(Add), BindingFlags.Static | BindingFlags.Public));
        core.NeoLuaEnvironment.SetValue("add", (Func<double, double, double>)Add);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        core.Dispose();
        core = default!;
        GC.Collect();
    }

    [Benchmark(Description = "MoonSharp (RunString)")]
    public DynValue Benchmark_MoonSharp_String()
    {
        return core.MoonSharpState.DoString(core.SourceText);
    }

    [Benchmark(Description = "MoonSharp (RunFile)")]
    public DynValue Benchmark_MoonSharp_File()
    {
        return core.MoonSharpState.DoFile(core.FilePath);
    }

    [Benchmark(Description = "NLua (DoString)")]
    public object[] Benchmark_NLua_String()
    {
        return core.NLuaState.DoString(core.SourceText);
    }

    [Benchmark(Description = "NLua (DoFile)")]
    public object[] Benchmark_NLua_File()
    {
        return core.NLuaState.DoFile(core.FilePath);
    }

    [Benchmark(Description = "NeoLua (DoChunk(code))")]
    public Neo.IronLua.LuaResult Benchmark_NeoLua_String()
    {
        return core.NeoLuaEnvironment.DoChunk(core.SourceText, "chunk");
    }

    [Benchmark(Description = "NeoLua (DoChunk(fileName))")]
    public Neo.IronLua.LuaResult Benchmark_NeoLua_File()
    {
        return core.NeoLuaEnvironment.DoChunk(core.FilePath);
    }

    [Benchmark(Description = "Lua-CSharp (DoString)")]
    public async Task<LuaValue> Benchmark_LuaCSharp_String()
    {
        await core.LuaCSharpState.DoStringAsync(core.SourceText, buffer);
        return buffer[0];
    }

    [Benchmark(Description = "Lua-CSharp (DoFileAsync)")]
    public async Task<LuaValue> Benchmark_LuaCSharp_File()
    {
        await core.LuaCSharpState.DoFileAsync(core.FilePath, buffer);
        return buffer[0];
    }
}