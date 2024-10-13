# Lua-CSharp

High performance Lua interpreter implemented in C# for .NET and Unity

![img](docs/Header.png)

[![NuGet](https://img.shields.io/nuget/v/LuaCSharp.svg)](https://www.nuget.org/packages/Lua)
[![Releases](https://img.shields.io/github/release/AnnulusGames/Lua-CSharp.svg)](https://github.com/AnnulusGames/Lua-CSharp/releases)
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

[English]((./README.md)) | 日本語

## 概要

Lua-CSharpはC#実装のLuaインタプリタを提供するライブラリです。Lua-CSharpを導入することで、.NETアプリケーション内で簡単にLuaスクリプトを組み込むことが可能になります。

// TODO: ベンチマークの追加

## 特徴

* C#で実装されたLua5.2インタプリタ
* async/awaitに統合された扱いやすいAPI
* try-catchによる例外処理のサポート
* 最新のC#を活用したハイパフォーマンスな実装
* Unityサポート(Mono/IL2CPPの両方で動作)

## インストール

### NuGet packages

Lua-CSharpを利用するには.NET Standard2.1以上が必要です。パッケージはNuGetから入手できます。

### .NET CLI

```ps1
dotnet add package LuaCSharp
```

### Package Manager

```ps1
Install-Package LuaCSharp
```

### Unity

Lua-CSharpをUnityで利用することも可能です。(Mono/IL2CPPの両方で動作します)

### 要件

* Unity 2021.3 以上

### インストール

1. [NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)をインストールします。
2. `NuGet > Manage NuGet Packages`からNuGetウィンドウを開き、`LuaCSharp`パッケージを検索してインストールします。

## クイックスタート

`LuaState`クラスを利用することでC#上からLuaスクリプトを実行することが可能です。以下はLuaで記述された簡単な演算を評価するサンプルコードです。

```cs
using Lua;

// LuaStateを作成する
var state = LuaState.Create();

// DoStringAsyncで文字列のLuaスクリプトを実行する
var results = await state.DoStringAsync("return 1 + 1");

// 2
Console.WriteLine(results[0]);
```

> [!WARNING]
> `LuaState`はスレッドセーフではありません。同時に複数のスレッドからアクセスしないでください。

## LuaValue

Luaスクリプト上の値は`LuaValue`型で表現されます。`LuaValue`の値は`TryRead<T>(out T value)`または`Read<T>()`で読み取ることが可能です。

```cs
var results = await state.DoStringAsync("return 1 + 1");

// double
var value = results[0].Read<double>();
```

また、`Type`プロパティから値の型を取得できます。

```cs
var isNil = results[0].Type == LuaValueType.Nil;
```

Lua-C#間の型の対応を以下に示します。

| Lua        | C#               |
| ---------- | ---------------- |
| `nil`      | `LuaValue.Nil`   |
| `boolean`  | `bool`           |
| `string`   | `string`         |
| `number`   | `double`,`float` |
| `table`    | `LuaTable`       |
| `function` | `LuaFunction`    |
| `userdata` | `LuaUserData`    |
| `thread`   | `LuaThread`      |

C#側から`LuaValue`を作成する際には、変換可能な型の場合であれば暗黙的に`LuaValue`に変換されます。

```cs
LuaValue value;
value = 1.2;           // double   ->  LuaValue
value = "foo";         // string   ->  LuaValue
value = new LuaTable() // LuaTable ->  LuaValue
```

## LuaTable

Luaのテーブルは`LuaTable`型で表現されます。これは通常の`LuaValue[]`や`Dictionary<LuaValue, LuaValue>`のように使用できます。

```cs
// Lua側でテーブルを作成
var results = await state.DoStringAsync("return { a = 1, b = 2, c = 3 }")
var table1 = results[0].Read<LuaTable>();

// 1
Console.WriteLine(table1["a"]);

// テーブルを作成
results = await state.DoStringAsync("return { 1, 2, 3 }")
var table2 = results[0].Read<LuaTable>();

// 1 (Luaの配列は1-originであることに注意)
Console.WriteLine(table2[1]);
```

## グローバル環境

`state.Environment`からLuaのグローバル環境にアクセスできます。このテーブルを介して簡単にLua-C#間で値をやり取りすることが可能です。

```cs
// a = 10を設定
state.Environment["a"] = 10;

var results = await state.DoStringAsync("return a");

// 10
Console.WriteLine(results[0]);
```

## 標準ライブラリ

Luaの標準ライブラリを利用することも可能です。`state.OpenStandardLibraries()`を呼び出すことで、`LuaState`に標準ライブラリのテーブルを追加します。

```cs
using Lua;
using Lua.Standard;

var state = LuaState.Create();

// 標準ライブラリを追加
state.OpenStandardLibraries();

var results = await state.DoStringAsync("return math.pi");
Console.WriteLine(results[0]); // 3.141592653589793
```

標準ライブラリについては[Lua公式のマニュアル](https://www.lua.org/manual/5.2/manual.html#6)を参照してください。

> [!WARNING]
> Lua-CSharpは標準ライブラリの全ての関数をサポートしているわけではありません。詳細は[互換性](#互換性)の項目を参照してください。

## 関数

Luaの関数は`LuaFunction`型で表現されます。`LuaFunction`によってLuaの関数をC#側から呼び出したり、C#で定義した関数をLua側から呼び出したりすることが可能です。

### Luaの関数をC#から呼び出す

```lua
-- lua2cs.lua

local function add(a, b)
    return a + b
end

return add;
```

```cs
var results = await state.DoFileAsync("lua2cs.lua");
var func = results[0].Read<LuaFunction>();

// 引数を与えて関数を実行する
var funcResults = await func.InvokeAsync(state, [1, 2]);

// 3
Console.WriteLine(funcResults[0]);
```

### C#の関数をLua側から呼び出す

`LuaFunction.Create()`を用いてラムダ式からLuaFunctionを作成できます。

```cs
// グローバル環境に関数を追加
state.Environment["add"] = LuaFunction.Create((args, ct) =>
{
    return [args[0].Read<double>() + ]
});

// Luaスクリプトを実行
var results = await state.DoFileAsync("cs2lua.lua");

// 3
Console.WriteLine(results[i]);
```

```lua
-- cs2lua.lua

return add(1, 2)
```

ただし、`LuaFunction.Create()`で作成された関数は呼び出し時に余計なアロケーションを発生させます。最良のパフォーマンスを得るためには`LuaFunction`を継承したクラスを実装してください。

## コルーチン

Luaのコルーチンは`LuaThread`型で表現されます。

コルーチンはLuaスクリプト内で利用できるだけでなく、Luaで作成したコルーチンをC#で待機することも可能です。

```lua
-- coroutine.lua

local co = coroutine.create(function()
    for i = 1, 10 do
        print(i)
        coroutine.yield()
    end
end)

return co
```

```cs
var results = await state.DoFileAsync("coroutine.lua");
var co = results[0].Read<LuaThread>();

for (int i = 0; i < 10; i++)
{
    var resumeResults = await co.ResumeAsync(state);

    // coroutine.resume()と同様、成功時は最初の要素にtrue、それ以降に関数の戻り値を返す
    // 1, 2, 3, 4, ...
    Console.WriteLine(resumeResults[1]);
}
```

## モジュールの読み込み

Luaでは`require`関数を用いてモジュールを読み込むことができます。通常のLuaでは`package.searchers`の検索関数を用いてモジュールの管理を行いますが、Lua-CSharpでは代わりに`ILuaModuleLoader`がモジュール読み込みの機構として提供されています。

```cs
public interface ILuaModuleLoader
{
    bool Exists(string moduleName);
    ValueTask<LuaModule> LoadAsync(string moduleName, CancellationToken cancellationToken = default);
}
```

これを`LuaState.ModuleLoader`に設定することでモジュールの読み込み方法を変更することができます。デフォルトのLoaderにはluaファイルからモジュールをロードする`FileModuleLoader`が設定されています。

また、`CompositeModuleLoader.Create(loader1, loader2, ...)`を利用することで複数のLoaderを組み合わせたLoaderを作成できます。

```cs
state.ModuleLoader = CompositeModuleLoader.Create(
    new FileModuleLoader(),
    new CustomModuleLoader()
);
```

また、ロード済みのモジュールは通常のLua同様に`package.loaded`テーブルにキャッシュされます。これは`LuaState.LoadedModules`からアクセスすることが可能です。

## 例外処理

Luaスクリプトの解析エラーや実行時例外は`LuaException`を継承した例外をスローします。これをcatchすることでエラー時の処理を行うことができます。

```cs
try
{
    await state.DoFileAsync("filename.lua");
}
catch (LuaParseException)
{
    // 構文にエラーがあった際の処理
}
catch (LuaRuntimeException)
{
    // 実行時例外が発生した際の処理
}
```

## 互換性

Lua-CSharpは.NETとの統合を念頭に設計されているため、C実装とは互換性がない仕様がいくつか存在します。

### バイナリ

Lua-CSharpはLuaバイトコードをサポートしません(luacなどは使用できません)。実行可能なのはLuaソースコードのみです。

### 文字コード

Lua-CSharpで利用される文字コードはUTF-16です。通常Luaは1バイトで1文字を表すエンコーディングを前提としているため、文字列まわりの動作が大きく異なります。

例えば、以下のコードの出力結果は通常のLuaでは`15`ですが、Lua-CSharpでは`5`です。

```lua
local l = string.len("あいうえお")
print(l)
```

Stringライブラリの関数はすべて文字列をUTF-16として扱う実装に変更されていることに注意してください。

### ガベージコレクション

Lua-CSharpはC#で実装されているため.NETのGCに依存しています。そのため、メモリ管理に関する動作が通常のLuaとは異なります。

`collectgarbage()`は利用可能ですが、これは単に`GC.Collect()`の呼び出しです。引数の値は無視されます。また、弱参照テーブル(week tables)はサポートされません。

### moduleライブラリ

moduleライブラリは`require()`および`package.loaded`のみが利用でき、それ以外の関数は実装されていません。これはLua-CSharpは.NETに最適化された独自のモジュール読み込みの機能を有するためです。

詳細は[モジュールの読み込み](#モジュールの読み込み)の項目を参照してください。

### debugライブラリ

現在debugライブラリの実装は提供されていません。これはLua-CSharpの内部実装がC実装とは大きく異なり、同じAPIのライブラリを提供することが難しいためです。これについては、v1までに実装可能な一部のAPIのみの提供、または代替となるデバッグ機能を検討しています。

## ライセンス

このライブラリは[MITライセンス](LICENSE)の下で提供されています。