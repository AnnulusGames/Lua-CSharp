using Lua.CodeAnalysis.Syntax;
using Lua.CodeAnalysis.Compilation;
using Lua.Runtime;
using Lua;
using Lua.Standard;
using System.Reflection;

var state = LuaState.Create();
state.OpenStandardLibraries();

state.Environment["wait"] = LuaFunction.Create(async (@args, ct) =>
{
    await Task.Delay(TimeSpan.FromSeconds(args[0].Read<double>()), ct);
    return default!;
});

state.Environment["dumpframe"] = LuaFunction.Create((@args, ct) =>
{
    var thread = state.CurrentThread;
    Console.WriteLine(thread.GetCallStackFrames()[^2]);
    return default;
});

state.Environment["dumpstack"] = LuaFunction.Create((@args, ct) =>
{
    var thread = state.CurrentThread;
    thread.GetType().GetMethod("DumpStackValues", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(thread, null);
    return default;
});

try
{
    var source = File.ReadAllText("test.lua");

    var syntaxTree = LuaSyntaxTree.Parse(source, "test.lua");

    Console.WriteLine("Source Code " + new string('-', 50));

    var debugger = new DisplayStringSyntaxVisitor();
    Console.WriteLine(debugger.GetDisplayString(syntaxTree));

    var chunk = LuaCompiler.Default.Compile(syntaxTree, "test.lua");

    DebugChunk(chunk, 0);

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

static void DebugChunk(Chunk chunk, int id)
{
    Console.WriteLine($"Chunk[{id}]" + new string('=', 50));
    Console.WriteLine($"Parameters:{chunk.ParameterCount}");

    Console.WriteLine("Instructions " + new string('-', 50));
    var index = 0;
    foreach (var inst in chunk.Instructions.ToArray())
    {
        Console.WriteLine($"[{index}]\t{chunk.SourcePositions[index]}\t\t{inst}");
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

    var nestedChunkId = 0;
    foreach (var localChunk in chunk.Functions)
    {
        DebugChunk(localChunk, nestedChunkId);
        nestedChunkId++;
    }
}