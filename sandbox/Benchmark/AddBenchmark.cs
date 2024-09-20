using System.Buffers;
using BenchmarkDotNet.Attributes;
using Lua;
using MoonSharp.Interpreter;

[Config(typeof(BenchmarkConfig))]
public class AddBenchmark
{
    NLua.Lua nLuaState = default!;
    Script moonSharpState = default!;
    LuaState luaCSharpState = default!;

    string filePath = default!;
    string sourceText = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // moonsharp
        moonSharpState = new Script();
        Script.WarmUp();

        // NLua
        nLuaState = new();

        // Lua-CSharp
        luaCSharpState = LuaState.Create();

        filePath = FileHelper.GetAbsolutePath("add.lua");
        sourceText = File.ReadAllText(filePath);
    }

    [Benchmark(Description = "MoonSharp (RunString)")]
    public DynValue Benchmark_MoonSharp_String()
    {
        var result = moonSharpState.DoString(sourceText);
        return result;
    }

    [Benchmark(Description = "MoonSharp (RunFile)")]
    public DynValue Benchmark_MoonSharp_File()
    {
        var result = moonSharpState.DoFile(filePath);
        return result;
    }

    [Benchmark(Description = "NLua (DoString)")]
    public object[] Benchmark_NLua_String()
    {
        return nLuaState.DoString(sourceText);
    }

    [Benchmark(Description = "NLua (DoFile)")]
    public object[] Benchmark_NLua_File()
    {
        return nLuaState.DoFile(filePath);
    }

    [Benchmark(Description = "Lua-CSharp (DoString)")]
    public async Task<LuaValue> Benchmark_LuaCSharp_String()
    {
        var buffer = ArrayPool<LuaValue>.Shared.Rent(1);
        try
        {
            await luaCSharpState.DoStringAsync(sourceText, buffer);
            return buffer[0];
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(buffer);
        }
    }

    [Benchmark(Description = "Lua-CSharp (DoFileAsync)")]
    public async Task<LuaValue> Benchmark_LuaCSharp_File()
    {
        var buffer = ArrayPool<LuaValue>.Shared.Rent(1);
        try
        {
            await luaCSharpState.DoFileAsync(filePath, buffer);
            return buffer[0];
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(buffer);
        }
    }
}