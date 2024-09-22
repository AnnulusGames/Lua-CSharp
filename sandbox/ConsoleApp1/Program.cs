using Lua.CodeAnalysis.Syntax;
using Lua.CodeAnalysis.Compilation;
using Lua.Runtime;
using Lua;
using Lua.Standard;

var state = LuaState.Create();
state.OpenBasicLibrary();

try
{
    var source =
"""
-- メインコルーチンの定義
local co_main = coroutine.create(function ()
    print("Main coroutine starts")

    -- コルーチンAの定義
    local co_a = coroutine.create(function()
        for i = 1, 3 do
            print("Coroutine A, iteration "..i)
            coroutine.yield()
        end
        print("Coroutine A ends")
    end)

    --コルーチンBの定義
    local co_b = coroutine.create(function()
        print("Coroutine B starts")
        coroutine.yield()-- 一時停止
        print("Coroutine B resumes")
    end)

    -- コルーチンCの定義(コルーチンBを呼び出す)
    local co_c = coroutine.create(function()
        print("Coroutine C starts")
        coroutine.resume(co_b)-- コルーチンBを実行
        print("Coroutine C calls B and resumes")
        coroutine.yield()-- 一時停止
        print("Coroutine C resumes")
    end)

    -- コルーチンAとCの交互実行
    for _ = 1, 2 do
            coroutine.resume(co_a)
        coroutine.resume(co_c)
    end

    -- コルーチンAを再開し完了させる
    coroutine.resume(co_a)

    -- コルーチンCを再開し完了させる
    coroutine.resume(co_c)

    print("Main coroutine ends")
end)

--メインコルーチンを開始
coroutine.resume(co_main)
""";

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