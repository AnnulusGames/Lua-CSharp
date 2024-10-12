using System.Runtime.CompilerServices;
using Lua.Internal;
using Lua.Runtime;

namespace Lua.Standard.Table;

public sealed class SortFunction : LuaFunction
{
    public override string Name => "sort";
    public static readonly SortFunction Instance = new();

    // TODO: optimize
    readonly Chunk defaultComparer = new()
    {
        Name = "comp",
        Functions = [],
        Constants = [],
        Instructions = [
            Instruction.Le(1, 0, 1),
            Instruction.LoadBool(2, 1, 1),
            Instruction.LoadBool(2, 0, 0),
            Instruction.Return(2, 2),
        ],
        SourcePositions = [
            default, default, default, default,
        ],
        ParameterCount = 2,
        UpValues = [],
    };

    protected override async ValueTask<int> InvokeAsyncCore(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var arg0 = context.GetArgument<LuaTable>(0);
        var arg1 = context.HasArgument(1)
            ? context.GetArgument<LuaFunction>(1)
            : new Closure(context.State, defaultComparer);

        await QuickSortAsync(context, arg0.GetArrayMemory(), 0, arg0.ArrayLength - 1, arg1, cancellationToken);
        return 0;
    }

    static async ValueTask QuickSortAsync(LuaFunctionExecutionContext context, Memory<LuaValue> memory, int low, int high, LuaFunction comparer, CancellationToken cancellationToken)
    {
        if (low < high)
        {
            int pivotIndex = await PartitionAsync(context, memory, low, high, comparer, cancellationToken);
            await QuickSortAsync(context, memory, low, pivotIndex - 1, comparer, cancellationToken);
            await QuickSortAsync(context, memory, pivotIndex + 1, high, comparer, cancellationToken);
        }
    }

    static async ValueTask<int> PartitionAsync(LuaFunctionExecutionContext context, Memory<LuaValue> memory, int low, int high, LuaFunction comparer, CancellationToken cancellationToken)
    {
        using var methodBuffer = new PooledArray<LuaValue>(1);

        var pivot = memory.Span[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            context.State.Push(memory.Span[j]);
            context.State.Push(pivot);
            await comparer.InvokeAsync(context with
            {
                ArgumentCount = 2,
                FrameBase = context.Thread.Stack.Count - context.ArgumentCount,
            }, methodBuffer.AsMemory(), cancellationToken);

            if (methodBuffer[0].ToBoolean())
            {
                i++;
                Swap(memory.Span, i, j);
            }
        }

        Swap(memory.Span, i + 1, high);
        return i + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Swap(Span<LuaValue> span, int i, int j)
    {
        (span[i], span[j]) = (span[j], span[i]);
    }
}