using Lua.CodeAnalysis.Syntax;
using Lua.CodeAnalysis.Compilation;
using Lua.Runtime;
using Lua;
using Lua.Standard;

var state = LuaState.Create();
state.OpenBaseLibrary();

try
{
    var source =
@"
metatable = {
    __add = function(a, b)
        local t = { }

        for i = 1, #a do
            t[i] = a[i] + b[i]
        end

        return t
    end
}

local a = { 1, 2, 3 }
local b = { 4, 5, 6 }

setmetatable(a, metatable)

return a + b
";

    var syntaxTree = LuaSyntaxTree.Parse(source, "main.lua");

    Console.WriteLine("Source Code " + new string('-', 50));

    var debugger = new DisplayStringSyntaxVisitor();
    Console.WriteLine(debugger.GetDisplayString(syntaxTree));

    var chunk = LuaCompiler.Default.Compile(syntaxTree, "main.lua");

    var id = 0;
    DebugChunk(chunk, ref id);

    Console.WriteLine("Output " + new string('-', 50));

    var results = new LuaValue[64];
    var resultCount = await state.RunAsync(chunk, results);

    Console.WriteLine("Result " + new string('-', 50));

    for (int i = 0; i < resultCount; i++)
    {
        Console.WriteLine(results[i]);
    }

    Console.WriteLine("End " + new string('-', 50));
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

static void DebugChunk(Chunk chunk, ref int id)
{
    Console.WriteLine($"Chunk[{id++}]" + new string('=', 50));

    Console.WriteLine("Instructions " + new string('-', 50));
    var index = 0;
    foreach (var inst in chunk.Instructions.ToArray())
    {
        Console.WriteLine($"[{index}]\t{chunk.SourcePositions[index]}\t{inst}");
        index++;
    }

    Console.WriteLine("Constants " + new string('-', 50)); index = 0;
    foreach (var constant in chunk.Constants.ToArray())
    {
        Console.WriteLine($"[{index}]\t{constant}");
        index++;
    }

    Console.WriteLine("UpValues " + new string('-', 50)); index = 0;
    foreach (var upValue in chunk.UpValues.ToArray())
    {
        Console.WriteLine($"[{index}]\t{upValue.Name}\t{(upValue.IsInRegister ? 1 : 0)}\t{upValue.Index}");
        index++;
    }

    Console.WriteLine();

    foreach (var localChunk in chunk.Functions)
    {
        DebugChunk(localChunk, ref id);
    }
}