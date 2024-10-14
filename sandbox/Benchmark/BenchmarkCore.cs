using Lua;
using MoonSharp.Interpreter;

public class BenchmarkCore : IDisposable
{
    public NLua.Lua NLuaState => nLuaState;
    public Neo.IronLua.LuaGlobal NeoLuaEnvironment => neoLuaEnvironment;
    public Script MoonSharpState => moonSharpState;
    public LuaState LuaCSharpState => luaCSharpState;
    public string FilePath => filePath;
    public string SourceText => sourceText;

    NLua.Lua nLuaState = default!;
    Neo.IronLua.Lua neoLuaState = default!;
    Neo.IronLua.LuaGlobal neoLuaEnvironment = default!;
    Script moonSharpState = default!;
    LuaState luaCSharpState = default!;
    string filePath = default!;
    string sourceText = default!;

    public void Setup(string fileName)
    {
        // moonsharp
        moonSharpState = new Script();
        Script.WarmUp();

        // NLua
        nLuaState = new();

        // NeoLua
        neoLuaState = new();
        neoLuaEnvironment = neoLuaState.CreateEnvironment();

        // Lua-CSharp
        luaCSharpState = LuaState.Create();

        filePath = FileHelper.GetAbsolutePath(fileName);
        sourceText = File.ReadAllText(filePath);
    }

    public void Dispose()
    {
        nLuaState.Dispose();
        neoLuaState.Dispose();
    }
}