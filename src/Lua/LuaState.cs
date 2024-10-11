using System.Diagnostics.CodeAnalysis;
using Lua.Internal;
using Lua.Loaders;
using Lua.Runtime;

namespace Lua;

public sealed class LuaState
{
    public const string DefaultChunkName = "chunk";

    // states
    readonly LuaMainThread mainThread = new();
    FastListCore<UpValue> openUpValues;
    FastStackCore<LuaThread> threadStack;
    readonly LuaTable environment;
    readonly UpValue envUpValue;
    bool isRunning;

    internal UpValue EnvUpValue => envUpValue;
    internal ref FastStackCore<LuaThread> ThreadStack => ref threadStack;
    internal ref FastListCore<UpValue> OpenUpValues => ref openUpValues;

    public LuaTable Environment => environment;
    public LuaMainThread MainThread => mainThread;
    public LuaThread CurrentThread
    {
        get
        {
            if (threadStack.TryPeek(out var thread)) return thread;
            return mainThread;
        }
    }

    public ILuaModuleLoader ModuleLoader { get; set; } = FileModuleLoader.Instance;

    // metatables
    LuaTable? nilMetatable;
    LuaTable? numberMetatable;
    LuaTable? stringMetatable;
    LuaTable? booleanMetatable;
    LuaTable? functionMetatable;
    LuaTable? threadMetatable;

    public static LuaState Create()
    {
        return new();
    }

    LuaState()
    {
        environment = new();
        envUpValue = UpValue.Closed(environment);
    }

    public async ValueTask<int> RunAsync(Chunk chunk, Memory<LuaValue> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfRunning();

        Volatile.Write(ref isRunning, true);
        try
        {
            var closure = new Closure(this, chunk);
            return await closure.InvokeAsync(new()
            {
                State = this,
                Thread = CurrentThread,
                ArgumentCount = 0,
                FrameBase = 0,
                SourcePosition = null,
                RootChunkName = chunk.Name ?? DefaultChunkName,
                ChunkName = chunk.Name ?? DefaultChunkName,
            }, buffer, cancellationToken);
        }
        finally
        {
            Volatile.Write(ref isRunning, false);
        }
    }

    public void Push(LuaValue value)
    {
        CurrentThread.Stack.Push(value);
    }

    public Traceback GetTraceback()
    {
        // TODO: optimize
        return new()
        {
            StackFrames = threadStack.AsSpan().ToArray()
                .Append(MainThread)
                .SelectMany(x => x.GetCallStackFrames()[1..].ToArray())
                .ToArray()
        };
    }

    internal bool TryGetMetatable(LuaValue value, [NotNullWhen(true)] out LuaTable? result)
    {
        result = value.Type switch
        {
            LuaValueType.Nil => nilMetatable,
            LuaValueType.Boolean => booleanMetatable,
            LuaValueType.String => stringMetatable,
            LuaValueType.Number => numberMetatable,
            LuaValueType.Function => functionMetatable,
            LuaValueType.Thread => threadMetatable,
            LuaValueType.UserData => value.Read<LuaUserData>().Metatable,
            LuaValueType.Table => value.Read<LuaTable>().Metatable,
            _ => null
        };

        return result != null;
    }

    internal void SetMetatable(LuaValue value, LuaTable metatable)
    {
        switch (value.Type)
        {
            case LuaValueType.Nil:
                nilMetatable = metatable;
                break;
            case LuaValueType.Boolean:
                booleanMetatable = metatable;
                break;
            case LuaValueType.String:
                stringMetatable = metatable;
                break;
            case LuaValueType.Number:
                numberMetatable = metatable;
                break;
            case LuaValueType.Function:
                functionMetatable = metatable;
                break;
            case LuaValueType.Thread:
                threadMetatable = metatable;
                break;
            case LuaValueType.UserData:
                value.Read<LuaUserData>().Metatable = metatable;
                break;
            case LuaValueType.Table:
                value.Read<LuaTable>().Metatable = metatable;
                break;
        }
    }

    internal UpValue GetOrAddUpValue(LuaThread thread, int registerIndex)
    {
        foreach (var upValue in openUpValues.AsSpan())
        {
            if (upValue.RegisterIndex == registerIndex && upValue.Thread == thread)
            {
                return upValue;
            }
        }

        var newUpValue = UpValue.Open(thread, registerIndex);
        openUpValues.Add(newUpValue);
        return newUpValue;
    }

    internal void CloseUpValues(LuaThread thread, int frameBase)
    {
        for (int i = 0; i < openUpValues.Length; i++)
        {
            var upValue = openUpValues[i];
            if (upValue.Thread != thread) continue;

            if (upValue.RegisterIndex >= frameBase)
            {
                upValue.Close();
                openUpValues.RemoveAtSwapback(i);
                i--;
            }
        }
    }

    void ThrowIfRunning()
    {
        if (Volatile.Read(ref isRunning))
        {
            throw new InvalidOperationException("the lua state is currently running");
        }
    }
}
