using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lua.Runtime;

public static partial class LuaVirtualMachine
{
    [StructLayout(LayoutKind.Auto)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    struct VirtualMachineExecutionContext(LuaState state, LuaStack stack, LuaValue[] resultsBuffer, Memory<LuaValue> buffer, LuaThread thread, Chunk chunk, CallStackFrame frame, CancellationToken cancellationToken)
    {
        public readonly LuaState State = state;
        public readonly LuaStack Stack = stack;
        public readonly Closure Closure = (Closure)frame.Function;
        public readonly LuaValue[] ResultsBuffer = resultsBuffer;
        public readonly Memory<LuaValue> Buffer = buffer;
        public readonly LuaThread Thread = thread;
        public readonly Chunk Chunk = chunk;
        public readonly Chunk RootChunk = chunk.GetRoot();
        public readonly CallStackFrame Frame = frame;
        public readonly CancellationToken CancellationToken = cancellationToken;
        public int Pc = -1;
        public Instruction Instruction;
        public bool Pushing;
        public int? ResultCount;
        public int TaskResult;
        public ValueTask<int> Task;
    }

    delegate void PostOperation(ref VirtualMachineExecutionContext context);

    delegate PostOperation? VMOperation(ref VirtualMachineExecutionContext context);

    static readonly VMOperation[] operations = new VMOperation[64];

    static readonly PostOperation nopOperation = static (ref VirtualMachineExecutionContext _) => { };

    [AsyncStateMachine(typeof(AsyncStateMachine))]
    internal static ValueTask<int> ExecuteClosureAsync(LuaState state, CallStackFrame frame, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = state.CurrentThread;
        var closure = (Closure)frame.Function;
        var chunk = closure.Proto;
        var resultBuffer = ArrayPool<LuaValue>.Shared.Rent(1024);

        var context = new VirtualMachineExecutionContext(state, thread.Stack, resultBuffer, buffer, thread, chunk, frame, cancellationToken);

        var stateMachine = new AsyncStateMachine
        {
            Context = context,
            Builder = new()
        };
        stateMachine.Builder.Start(ref stateMachine);
        return stateMachine.Builder.Task;
    }
    
    
    // //Asynchronous method implementation. 
    // internal async static ValueTask<int> ExecuteClosureAsync(LuaState state, CallStackFrame frame, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    // {
    //     var thread = state.CurrentThread;
    //     var closure = (Closure)frame.Function;
    //     var chunk = closure.Proto;
    //     var resultBuffer = ArrayPool<LuaValue>.Shared.Rent(1024);
    //
    //     var context = new VirtualMachineExecutionContext(state, thread.Stack, resultBuffer, buffer, thread, chunk, frame, cancellationToken);
    //     try
    //     {
    //         var instructions = chunk.Instructions;
    //
    //         while (context.ResultCount == null)
    //         {
    //             var instruction = instructions[++context.Pc];
    //             context.Instruction = instruction;
    //             var operation = operations[(int)instruction.OpCode];
    //             var action = operation(ref context);
    //             if (action != null)
    //             {
    //                 context.TaskResult = await context.Task;
    //                 //if (context.Pushing) //Assuming context.Pushing is always true
    //                 {
    //                     context.Thread.PopCallStackFrame();
    //                     context.Pushing = false;
    //                 }
    //                 action(ref context);
    //             }
    //         }
    //
    //         return context.ResultCount.Value;
    //     }
    //     catch (Exception)
    //     {
    //         if (context.Pushing) context.Thread.PopCallStackFrame();
    //         context.State.CloseUpValues(context.Thread, context.Frame.Base);
    //         throw;
    //     }
    //     finally
    //     {
    //         ArrayPool<LuaValue>.Shared.Return(context.ResultsBuffer);
    //     }
    // }
    
    /// <summary>
    /// Manual implementation of the async state machine
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    struct AsyncStateMachine : IAsyncStateMachine
    {
        enum State
        {
            Running = 0,
            //Await is the state where the task is awaited
            Await,
            //End is the state where the function is done
            End
        }

        public VirtualMachineExecutionContext Context;
        public AsyncValueTaskMethodBuilder<int> Builder;
        State state;
        ValueTaskAwaiter<int> awaiter;
        PostOperation? postOperation;

        public void MoveNext()
        {
            ref var context = ref Context;
            try
            {
                //If the state is await, task is done, so this executes the post operation and set the state to running
                if (state == State.Await)
                {
                    context.TaskResult = awaiter.GetResult();
                    awaiter = default;
                    //Pop the call stack frame because the function task is done
                    context.Thread.PopCallStackFrame();
                    context.Pushing = false;
                    postOperation!(ref context);
                    postOperation = null;
                    state = State.Running;
                }
                //If the state is end, the function is done, so set the result and return. I think this state is not reachable in this implementation
                else if (state == State.End)
                {
                    Builder.SetResult(context.ResultCount ?? 0);
                    return;
                }
                var instructions = context.Chunk.Instructions;
                while (context.ResultCount == null)
                {
                    var instruction = instructions[++context.Pc];
                    context.Instruction = instruction;
                    var action = operations[(byte)instruction.OpCode](ref context);
                    //If the action is null, task is not returned so continue to the next instruction
                    if (action == null) continue;
                    awaiter = context.Task.GetAwaiter();
                    //Directly execute the action if the awaiter is already completed
                    if (awaiter.IsCompleted)
                    {
                        context.TaskResult = awaiter.GetResult();
                        awaiter = default;
                        context.Thread.PopCallStackFrame();
                        context.Pushing = false;

                        action(ref context);
                    }
                    //Otherwise, set the state to await and return with setting this method as the task's continuation
                    else
                    {
                        postOperation = action;
                        state = State.Await;
                        Builder.AwaitOnCompleted(ref awaiter, ref this);
                        return;
                    }
                }
                //If the result count is set, this function is done, so set the result and set the state to end
                state = State.End;
                ArrayPool<LuaValue>.Shared.Return(context.ResultsBuffer);
                Builder.SetResult(context.ResultCount.Value);
            }
            catch (Exception e)
            {
                if (context.Pushing) context.Thread.PopCallStackFrame();
                context.State.CloseUpValues(context.Thread, context.Frame.Base);
                ArrayPool<LuaValue>.Shared.Return(context.ResultsBuffer);
                state = State.End;
                context = default;
                Builder.SetException(e);
            }
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            Builder.SetStateMachine(stateMachine);
        }
    }

    static LuaVirtualMachine()
    {
        new Operator().SetOperations();
    }


    class Operator
    {
        public void SetOperations()
        {
            operations[(int)OpCode.Move] = Move;
            operations[(int)OpCode.LoadK] = LoadK;
            operations[(int)OpCode.LoadKX] = LoadKX;
            operations[(int)OpCode.LoadBool] = LoadBool;
            operations[(int)OpCode.LoadNil] = LoadNil;
            operations[(int)OpCode.GetUpVal] = GetUpVal;
            operations[(int)OpCode.GetTabUp] = GetTabUp;
            operations[(int)OpCode.GetTable] = GetTable;
            operations[(int)OpCode.SetTabUp] = SetTabUp;
            operations[(int)OpCode.SetUpVal] = SetUpVal;
            operations[(int)OpCode.SetTable] = SetTable;
            operations[(int)OpCode.NewTable] = NewTable;
            operations[(int)OpCode.Self] = Self;
            operations[(int)OpCode.Add] = Add;
            operations[(int)OpCode.Sub] = Sub;
            operations[(int)OpCode.Mul] = Mul;
            operations[(int)OpCode.Div] = Div;
            operations[(int)OpCode.Mod] = Mod;
            operations[(int)OpCode.Pow] = Pow;
            operations[(int)OpCode.Unm] = Unm;
            operations[(int)OpCode.Not] = Not;
            operations[(int)OpCode.Len] = Len;
            operations[(int)OpCode.Concat] = Concat;
            operations[(int)OpCode.Jmp] = Jmp;
            operations[(int)OpCode.Eq] = Eq;
            operations[(int)OpCode.Lt] = Lt;
            operations[(int)OpCode.Le] = Le;
            operations[(int)OpCode.Test] = Test;
            operations[(int)OpCode.TestSet] = TestSet;
            operations[(int)OpCode.Call] = Call;
            operations[(int)OpCode.TailCall] = TailCall;
            operations[(int)OpCode.Return] = Return;
            operations[(int)OpCode.ForLoop] = ForLoop;
            operations[(int)OpCode.ForPrep] = ForPrep;
            operations[(int)OpCode.TForCall] = TForCall;
            operations[(int)OpCode.TForLoop] = TForLoop;
            operations[(int)OpCode.SetList] = SetList;
            operations[(int)OpCode.Closure] = Closure;
            operations[(int)OpCode.VarArg] = VarArg;
            operations[(int)OpCode.ExtraArg] = ExtraArg;
        }


        PostOperation? Move(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var RA = instruction.A + context.Frame.Base;
            var stack = context.Stack;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var RB = instruction.B + context.Frame.Base;
            Unsafe.Add(ref stackHead, RA) = Unsafe.Add(ref stackHead, RB);
            stack.NotifyTop(RA + 1);
            return null;
        }

        PostOperation? LoadK(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var RA = instruction.A + context.Frame.Base;
            var stack = context.Stack;
            stack.EnsureCapacity(RA + 1);
            stack.Get(RA) = context.Chunk.Constants[instruction.Bx];
            stack.NotifyTop(RA + 1);
            return null;
        }

        PostOperation? LoadKX(ref VirtualMachineExecutionContext _) => throw new NotImplementedException();

        PostOperation? LoadBool(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            stack.Get(RA) = instruction.B != 0;
            stack.NotifyTop(RA + 1);
            if (instruction.C != 0) context.Pc++;
            return null;
        }

        PostOperation? LoadNil(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            var iB = instruction.B;
            stack.EnsureCapacity(RA + iB + 1);
            stack.GetBuffer().Slice(RA, iB + 1).Clear();
            stack.NotifyTop(RA + iB + 1);
            return null;
        }

        PostOperation? GetUpVal(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            var upValue = context.Closure.UpValues[instruction.B];
            stack.Get(RA) = upValue.GetValue();
            stack.NotifyTop(RA + 1);
            return null;
        }

        PostOperation? GetTabUp(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var frame = context.Frame;
            var RA = instruction.A + frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vc = RK(ref stackHead, chunk, instruction.C, frame.Base);
            var upValue = context.Closure.UpValues[instruction.B];
            var table = upValue.GetValue();
            var isTable = table.TryReadTable(out var t);

            if (isTable && t.TryGetValue(vc, out var result))
            {
                var value = result;
                Unsafe.Add(ref stackHead, RA) = value;
                stack.NotifyTop(RA + 1);
            }
            else if (TryGetMetaTableValue(table, vc, ref context))
            {
                return static (ref VirtualMachineExecutionContext context) =>
                {
                    var stack = context.Stack;
                    var instruction = context.Instruction;
                    var RA = instruction.A + context.Frame.Base;
                    stack.Get(RA) = context.ResultsBuffer[0];
                    stack.NotifyTop(RA + 1);
                };
            }
            else if (isTable)
            {
                var value = LuaValue.Nil;
                Unsafe.Add(ref stackHead, RA) = value;
                stack.NotifyTop(RA + 1);
            }
            else
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "index", table);
            }

            return null;
        }

        PostOperation? GetTable(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var frame = context.Frame;
            var RA = instruction.A + frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var RB = instruction.B + frame.Base;
            var table = Unsafe.Add(ref stackHead, RB);
            var vc = RK(ref stackHead, context.Chunk, instruction.C, frame.Base);
            var isTable = table.TryReadTable(out var t);

            if (isTable && t.TryGetValue(vc, out var result))
            {
                var value = result;
                Unsafe.Add(ref stackHead, RA) = value;
                stack.NotifyTop(RA + 1);
            }
            else if (TryGetMetaTableValue(table, vc, ref context))
            {
                return static (ref VirtualMachineExecutionContext context) =>
                {
                    var stack = context.Stack;
                    var instruction = context.Instruction;
                    var RA = instruction.A + context.Frame.Base;
                    stack.Get(RA) = context.ResultsBuffer[0];
                    stack.NotifyTop(RA + 1);
                };
            }
            else if (isTable)
            {
                var value = LuaValue.Nil;
                stack.Get(RA) = value;
                stack.NotifyTop(RA + 1);
            }
            else
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(context.State, context.Chunk, context.Pc), "index", table);
            }

            return null;
        }

        PostOperation? SetTabUp(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var frame = context.Frame;
            ref var stackHead = ref stack.Get(0);
            var vb = RK(ref stackHead, context.Chunk, instruction.B, frame.Base);
            if (vb.TryReadNumber(out var d))
            {
                if (double.IsNaN(d))
                {
                    throw new LuaRuntimeException(GetTracebacks(context.State, context.Chunk, context.Pc), "table index is NaN");
                }
            }

            var vc = RK(ref stackHead, context.Chunk, instruction.C, frame.Base);
            var upValue = context.Closure.UpValues[instruction.A];
            var table = upValue.GetValue();

            if (table.TryReadTable(out var t))
            {
                t[vb] = vc;
            }
            else if (TrySetMetaTableValue(table, vb, vc, ref context))
            {
                return nopOperation;
            }
            else
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "index", table);
            }

            return null;
        }

        PostOperation? SetUpVal(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            var upValue = context.Closure.UpValues[instruction.B];
            upValue.SetValue(stack.Get(RA));
            return null;
        }

        PostOperation? SetTable(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            ref var stackHead = ref stack.Get(0);
            var vb = RK(ref stackHead, context.Chunk, instruction.B, context.Frame.Base);
            if (vb.TryReadNumber(out var d))
            {
                if (double.IsNaN(d))
                {
                    throw new LuaRuntimeException(GetTracebacks(ref context), "table index is NaN");
                }
            }

            var vc = RK(ref stackHead, context.Chunk, instruction.C, context.Frame.Base);
            var table = Unsafe.Add(ref stackHead, RA);

            if (table.TryReadTable(out var t))
            {
                t[vb] = vc;
            }
            else if (TrySetMetaTableValue(table, vb, vc, ref context))
            {
                return nopOperation;
            }
            else
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "index", table);
            }

            return null;
        }

        PostOperation? NewTable(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            stack.Get(RA) = new LuaTable(instruction.B, instruction.C);
            stack.NotifyTop(RA + 1);
            return null;
        }

        PostOperation? Self(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 2);
            ref var stackHead = ref stack.Get(0);
            var RB = instruction.B + context.Frame.Base;
            var table = Unsafe.Add(ref stackHead, RB);
            var vc = RK(ref stackHead, context.Chunk, instruction.C, context.Frame.Base);
            var isTable = table.TryReadTable(out var t);

            if (isTable && t.TryGetValue(vc, out var result))
            {
                Unsafe.Add(ref stackHead, RA + 1) = table;
                Unsafe.Add(ref stackHead, RA) = result;
                stack.NotifyTop(RA + 2);
            }
            else if (TryGetMetaTableValue(table, vc, ref context))
            {
                return static (ref VirtualMachineExecutionContext context) =>
                {
                    var stack = context.Stack;
                    var instruction = context.Instruction;
                    var RA = instruction.A + context.Frame.Base;
                    var RB = instruction.B + context.Frame.Base;
                    ref var stackHead = ref stack.Get(0);
                    var table = Unsafe.Add(ref stackHead, RB);
                    Unsafe.Add(ref stackHead, RA + 1) = table;
                    var value = context.ResultsBuffer[0];
                    Unsafe.Add(ref stackHead, RA) = value;
                    stack.NotifyTop(RA + 2);
                };
            }
            else if (isTable)
            {
                Unsafe.Add(ref stackHead, RA + 1) = table;
                Unsafe.Add(ref stackHead, RA) = LuaValue.Nil;
                stack.NotifyTop(RA + 2);
            }
            else
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "index", table);
            }

            return null;
        }

        PostOperation? Add(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadDouble(out var valueB) && vc.TryReadDouble(out var valueC))
            {
                Unsafe.Add(ref stackHead, RA) = valueB + valueC;
                stack.NotifyTop(RA + 1);
                return null;
            }
            
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Add, "add");
        }

        PostOperation? Sub(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadDouble(out var valueB) && vc.TryReadDouble(out var valueC))
            {
                Unsafe.Add(ref stackHead, RA) = valueB - valueC;
                stack.NotifyTop(RA + 1);
                return null;
            }
            
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Sub, "sub");
        }

        PostOperation? Mul(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadDouble(out var valueB) && vc.TryReadDouble(out var valueC))
            {
                Unsafe.Add(ref stackHead, RA) = valueB * valueC;
                stack.NotifyTop(RA + 1);
                return null;
            }
            
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Mul, "mul");
        }

        PostOperation? Div(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadDouble(out var valueB) && vc.TryReadDouble(out var valueC))
            {
                Unsafe.Add(ref stackHead, RA) = valueB / valueC;
                stack.NotifyTop(RA + 1);
                return null;
            }
            
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Div, "div");
        }

        PostOperation? Mod(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadDouble(out var valueB) && vc.TryReadDouble(out var valueC))
            {
                var mod = valueB % valueC;
                if ((valueC > 0 && mod < 0) || (valueC < 0 && mod > 0))
                {
                    mod += valueC;
                }

                Unsafe.Add(ref stackHead, RA) = mod;
                stack.NotifyTop(RA + 1);
                return null;
            }
            
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Mod, "mod");
        }

        PostOperation? Pow(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadDouble(out var valueB) && vc.TryReadDouble(out var valueC))
            {
                Unsafe.Add(ref stackHead, RA) = Math.Pow(valueB, valueC);
                stack.NotifyTop(RA + 1);
                return null;
            }
            
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Pow, "pow");
        }

        PostOperation? Unm(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var RB = instruction.B + context.Frame.Base;
            var vb = Unsafe.Add(ref stackHead, RB);

            if (vb.TryReadDouble(out var valueB))
            {
                Unsafe.Add(ref stackHead, RA) = -valueB;
                stack.NotifyTop(RA + 1);
                return null;
            }
            return ExecuteUnaryOperationMetaMethod(vb, ref context, Metamethods.Unm, "unm",false);
        }

        PostOperation? Not(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var RB = instruction.B + context.Frame.Base;
            Unsafe.Add(ref stackHead, RA) = !Unsafe.Add(ref stackHead, RB).ToBoolean();
            stack.NotifyTop(RA + 1);

            return null;
        }

        PostOperation? Len(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var RB = instruction.B + context.Frame.Base;
            var vb = Unsafe.Add(ref stackHead, RB);

            if (vb.TryReadString(out var str))
            {
                Unsafe.Add(ref stackHead, RA) = str.Length;
                stack.NotifyTop(RA + 1);
                return null;
            }
            return ExecuteUnaryOperationMetaMethod(vb, ref context, Metamethods.Len, "get length of",true);
        }

        PostOperation? Concat(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            var bIsValid = vb.TryReadString(out var strB);
            var cIsValid = vc.TryReadString(out var strC);

            if (!bIsValid && vb.TryReadDouble(out var numB))
            {
                strB = numB.ToString();
                bIsValid = true;
            }

            if (!cIsValid && vc.TryReadDouble(out var numC))
            {
                strC = numC.ToString();
                cIsValid = true;
            }

            if (bIsValid && cIsValid)
            {
                stack.Get(RA) = strB + strC;
                stack.NotifyTop(RA + 1);
                return null;
            }
            return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Concat, "concat");
        }

        PostOperation? Jmp(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            context.Pc += instruction.SBx;
            if (instruction.A != 0)
            {
                context.State.CloseUpValues(context.Thread, instruction.A - 1);
            }

            return null;
        }

        PostOperation? Eq(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);
            
            if (vb == vc)
            {
                if (instruction.A != 1)
                {
                    context.Pc++;
                }

                return null;
            }
            
            return ExecuteCompareOperationMetaMethod(vb, vc, ref context,Metamethods.Eq,null);
        }

        PostOperation? Lt(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadString(out var strB) && vc.TryReadString(out var strC))
            {
               var compareResult = StringComparer.Ordinal.Compare(strB, strC) < 0;
               if (compareResult != (instruction.A == 1))
               {
                   context.Pc++;
               }

               return null;
            }

            if (vb.TryReadNumber(out var valueB) && vc.TryReadNumber(out var valueC))
            {
               var  compareResult = valueB < valueC;
               if (compareResult != (instruction.A == 1))
               {
                   context.Pc++;
               }

               return null;
            }
            
            return ExecuteCompareOperationMetaMethod(vb, vc, ref context,Metamethods.Lt,"less than");
        }

        PostOperation? Le(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            stack.EnsureCapacity(RA + 1);
            ref var stackHead = ref stack.Get(0);
            var chunk = context.Chunk;
            var vb = RK(ref stackHead, chunk, instruction.B, context.Frame.Base);
            var vc = RK(ref stackHead, chunk, instruction.C, context.Frame.Base);

            if (vb.TryReadString(out var strB) && vc.TryReadString(out var strC))
            {
                var compareResult = StringComparer.Ordinal.Compare(strB, strC) <= 0; 
                if (compareResult != (instruction.A == 1))
                {
                    context.Pc++;
                }

                return null;
            }
            
            if (vb.TryReadNumber(out var valueB) && vc.TryReadNumber(out var valueC))
            {
                var  compareResult = valueB <= valueC;
                if (compareResult != (instruction.A == 1))
                {
                    context.Pc++;
                }

                return null;
            }
            
            return ExecuteCompareOperationMetaMethod(vb, vc, ref context,Metamethods.Le,"less than or equals");
        }

        PostOperation? Test(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            if (stack.Get(RA).ToBoolean() != (instruction.C == 1))
            {
                context.Pc++;
            }

            return null;
        }

        PostOperation? TestSet(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            var RB = instruction.B + context.Frame.Base;
            if (stack.Get(RB).ToBoolean() != (instruction.C == 1))
            {
                context.Pc++;
            }
            else
            {
                stack.Get(RA) = stack.Get(RB);
                stack.NotifyTop(RA + 1);
            }

            return null;
        }

        PostOperation? Call(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            var va = stack.Get(RA);
            if (!va.TryReadFunction(out var func))
            {
                if (va.TryGetMetamethod(context.State, Metamethods.Call, out var metamethod) && metamethod.TryReadFunction(out func))
                {
                }
                else
                {
                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
                }
            }

            var chunk = context.Chunk;
            var thread = context.Thread;
            var (newBase, argumentCount) = PrepareForFunctionCall(thread, func, instruction, RA, false);

            var callPosition = chunk.SourcePositions[context.Pc];
            var chunkName = chunk.Name ?? LuaState.DefaultChunkName;
            var rootChunkName = context.RootChunk.Name ?? LuaState.DefaultChunkName;

            var callStackFrame = new CallStackFrame
            {
                Base = newBase,
                CallPosition = callPosition,
                ChunkName = chunkName,
                RootChunkName = rootChunkName,
                VariableArgumentCount = func is Closure cl ? Math.Max(argumentCount - cl.Proto.ParameterCount, 0) : 0,
                Function = func,
            };
            thread.PushCallStackFrame(in callStackFrame);


            context.Pushing = true;
            context.Task = func.Func(new()
            {
                State = context.State,
                Thread = thread,
                ArgumentCount = argumentCount,
                FrameBase = newBase,
                SourcePosition = callPosition,
                ChunkName = chunkName,
                RootChunkName = rootChunkName,
            }, context.ResultsBuffer.AsMemory(), context.CancellationToken);

            return static (ref VirtualMachineExecutionContext context) =>
            {
                var instruction = context.Instruction;
                var rawResultCount = context.TaskResult;
                var resultCount = rawResultCount;
                var ic = instruction.C;

                if (ic != 0)
                {
                    resultCount = ic - 1;
                }

                if (resultCount == 0)
                {
                    context.Stack.Pop();
                }
                else
                {
                    var stack = context.Stack;
                    var RA = instruction.A + context.Frame.Base;
                    stack.EnsureCapacity(RA + resultCount);
                    ref var stackHead = ref stack.Get(0);
                    var results = context.ResultsBuffer.AsSpan();
                    for (int i = 0; i < resultCount; i++)
                    {
                        Unsafe.Add(ref stackHead, RA + i) = i >= rawResultCount
                            ? LuaValue.Nil
                            : results[i];
                    }

                    stack.NotifyTop(RA + resultCount);
                }
            };
        }

        PostOperation? TailCall(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            var state = context.State;
            var thread = context.Thread;

            state.CloseUpValues(thread, context.Frame.Base);

            var va = stack.Get(RA);
            if (!va.TryReadFunction(out var func))
            {
                if (!va.TryGetMetamethod(state, Metamethods.Call, out var metamethod) && !metamethod.TryReadFunction(out func))
                {
                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
                }
            }

            var (newBase, argumentCount) = PrepareForFunctionCall(thread, func, instruction, RA, true);
            var rootChunk = context.RootChunk;

            context.Pushing = true;
            context.Task = func.InvokeAsyncPushOnly(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = argumentCount,
                FrameBase = newBase,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                ChunkName = context.Chunk.Name,
                RootChunkName = rootChunk.Name,
            }, context.Buffer, context.CancellationToken);

            return static (ref VirtualMachineExecutionContext context) => { context.ResultCount = context.TaskResult; };
        }

        PostOperation? Return(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;


            context.State.CloseUpValues(context.Thread, context.Frame.Base);
            var RA = instruction.A + context.Frame.Base;
            var retCount = instruction.B - 1;

            if (retCount == -1)
            {
                retCount = stack.Count - RA;
            }

            var buffer = context.Buffer.Span;
            for (int i = 0; i < retCount; i++)
            {
                buffer[i] = stack.Get(RA + i);
            }

            context.ResultCount = retCount;

            return null;
        }

        PostOperation? ForLoop(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            stack.EnsureCapacity(RA + 4);
            ref var RARef = ref Unsafe.Add(ref stack.Get(0), RA);

            if (!RARef.TryReadDouble(out var init))
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "'for' initial value must be a number");
            }

            if (!Unsafe.Add(ref RARef, 1).TryReadDouble(out var limit))
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "'for' limit must be a number");
            }

            if (!Unsafe.Add(ref RARef, 2).TryReadDouble(out var step))
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "'for' step must be a number");
            }

            var va = init + step;
            RARef = va;

            if (step >= 0 ? va <= limit : va >= limit)
            {
                context.Pc += instruction.SBx;
                Unsafe.Add(ref RARef, 3) = va;
                stack.NotifyTop(RA + 4);
            }
            else
            {
                stack.NotifyTop(RA + 1);
            }


            return null;
        }

        PostOperation? ForPrep(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            if (!stack.Get(RA).TryReadDouble(out var init))
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "'for' initial value must be a number");
            }

            if (!stack.Get(RA + 2).TryReadDouble(out var step))
            {
                throw new LuaRuntimeException(context.State.GetTraceback(), "'for' step must be a number");
            }

            stack.Get(RA) = init - step;
            stack.NotifyTop(RA + 1);
            context.Pc += instruction.SBx;


            return null;
        }

        PostOperation? TForCall(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            var iteratorRaw = stack.Get(RA);
            if (!iteratorRaw.TryReadFunction(out var iterator))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", iteratorRaw);
            }

            var nextBase = RA + 3 + instruction.C;
            stack.Get(nextBase) = stack.Get(RA + 1);
            stack.Get(nextBase + 1) = stack.Get(RA + 2);
            stack.NotifyTop(nextBase + 2);
            context.Pushing = true;
            context.Task = iterator.InvokeAsyncPushOnly(new()
            {
                State = context.State,
                Thread = context.Thread,
                ArgumentCount = 2,
                FrameBase = nextBase,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                ChunkName = context.Chunk.Name,
                RootChunkName = context.RootChunk.Name,
            }, context.ResultsBuffer.AsMemory(), context.CancellationToken);


            return (static (ref VirtualMachineExecutionContext context) =>
            {
                var stack = context.Stack;
                var instruction = context.Instruction;
                var RA = instruction.A + context.Frame.Base;
                var resultBuffer = context.ResultsBuffer;
                var resultCount = context.TaskResult;
                stack.EnsureCapacity(RA + instruction.C + 3);
                for (int i = 1; i <= instruction.C; i++)
                {
                    var index = i - 1;
                    stack.Get(RA + 2 + i) = index >= resultCount
                        ? LuaValue.Nil
                        : resultBuffer[i - 1];
                }

                stack.NotifyTop(RA + instruction.C + 3);
            });
        }

        PostOperation? TForLoop(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            var forState = stack.Get(RA + 1);
            if (forState.Type is not LuaValueType.Nil)
            {
                stack.Get(RA) = forState;
                context.Pc += instruction.SBx;
            }


            return null;
        }

        PostOperation? SetList(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            if (!stack.Get(RA).TryReadTable(out var table))
            {
                throw new LuaException("internal error");
            }

            var count = instruction.B == 0
                ? stack.Count - (RA + 1)
                : instruction.B;

            table.EnsureArrayCapacity((instruction.C - 1) * 50 + count);
            stack.AsSpan().Slice(RA + 1, count)
                .CopyTo(table.GetArraySpan()[((instruction.C - 1) * 50)..]);

            return null;
        }

        PostOperation? Closure(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;

            stack.EnsureCapacity(RA + 1);
            stack.Get(RA) = new Closure(context.State, context.Chunk.Functions[instruction.SBx]);
            stack.NotifyTop(RA + 1);

            return null;
        }

        PostOperation? VarArg(ref VirtualMachineExecutionContext context)
        {
            var instruction = context.Instruction;
            var stack = context.Stack;
            var RA = instruction.A + context.Frame.Base;
            var frame = context.Frame;
            var count = instruction.B == 0
                ? frame.VariableArgumentCount
                : instruction.B - 1;

            stack.EnsureCapacity(RA + count);
            for (int i = 0; i < count; i++)
            {
                stack.Get(RA + i) = frame.VariableArgumentCount > i
                    ? stack.Get(frame.Base - (frame.VariableArgumentCount - i))
                    : LuaValue.Nil;
            }

            stack.NotifyTop(RA + count);

            return null;
        }

        PostOperation? ExtraArg(ref VirtualMachineExecutionContext _) => throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static LuaValue RK(ref LuaValue stack, Chunk chunk, ushort index, int frameBase)
    {
        return index >= 256 ? chunk.Constants[index - 256] : Unsafe.Add(ref stack, index + frameBase);
    }

    static bool TryGetMetaTableValue(LuaValue table, LuaValue key, ref VirtualMachineExecutionContext context)
    {
        var state = context.State;
        if (table.TryGetMetamethod(state, Metamethods.Index, out var metamethod))
        {
            if (!metamethod.TryReadFunction(out var indexTable))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var stack = context.Stack;
            stack.Push(table);
            stack.Push(key);
            context.Pushing = true;

            context.Task = indexTable.InvokeAsyncPushOnly(new()
            {
                State = state,
                Thread = context.Thread,
                ArgumentCount = 2,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                FrameBase = stack.Count - 2,
                ChunkName = context.Chunk.Name,
                RootChunkName = context.RootChunk.Name,
            }, context.ResultsBuffer, context.CancellationToken);
            return true;
        }

        return false;
    }

    static bool TrySetMetaTableValue(LuaValue table, LuaValue key, LuaValue value, ref VirtualMachineExecutionContext context)
    {
        var state = context.State;
        if (table.TryGetMetamethod(state, Metamethods.NewIndex, out var metamethod))
        {
            if (!metamethod.TryReadFunction(out var indexTable))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var thread = context.Thread;
            var stack = thread.Stack;
            stack.Push(table);
            stack.Push(key);
            stack.Push(value);
            context.Pushing = true;
            context.Task = indexTable.InvokeAsyncPushOnly(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = 3,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                FrameBase = stack.Count - 3,
                ChunkName = context.Chunk.Name,
                RootChunkName = context.RootChunk.Name,
            }, context.ResultsBuffer, context.CancellationToken);
            return true;
        }

        return false;
    }

    static PostOperation? ExecuteBinaryOperationMetaMethod(LuaValue vb,LuaValue vc,ref VirtualMachineExecutionContext context, string name,string description)
    {
        if (vb.TryGetMetamethod(context.State, name, out var metamethod) || vc.TryGetMetamethod(context.State, name, out metamethod))
        {
            if (!metamethod.TryReadFunction(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var rootChunk = context.RootChunk;
            var state = context.State;
            var thread = context.Thread;
            var cancellationToken = context.CancellationToken;
            var stack = context.Stack;
            stack.Push(vb);
            stack.Push(vc);
            context.Pushing = true;
            context.Task = func.InvokeAsyncPushOnly(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = 2,
                FrameBase = stack.Count - 2,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                ChunkName = context.Chunk.Name,
                RootChunkName = rootChunk.Name,
            }, context.ResultsBuffer.AsMemory(), cancellationToken);

            return static (ref VirtualMachineExecutionContext context) =>
            {
                var stack = context.Stack;
                var RA = context.Instruction.A + context.Frame.Base;
                stack.Get(RA) = context.TaskResult == 0 ? LuaValue.Nil : context.ResultsBuffer[0];
                stack.NotifyTop(RA + 1);
            };
        }
            
        LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), description, vb, vc);
        return null;
    }
    
    static PostOperation? ExecuteUnaryOperationMetaMethod(LuaValue vb,ref VirtualMachineExecutionContext context, string name,string description,bool isLen)
    {
        var stack = context.Stack;
        if (vb.TryGetMetamethod(context.State, name, out var metamethod))
        {
            if (!metamethod.TryReadFunction(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var rootChunk = context.RootChunk;
            var state = context.State;
            var thread = context.Thread;
            var cancellationToken = context.CancellationToken;
           
            stack.Push(vb);
            context.Pushing = true;
            context.Task = func.InvokeAsyncPushOnly(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = 1,
                FrameBase = stack.Count - 1,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                ChunkName = context.Chunk.Name,
                RootChunkName = rootChunk.Name,
            }, context.ResultsBuffer.AsMemory(), cancellationToken);

            return static (ref VirtualMachineExecutionContext context) =>
            {
                var stack = context.Stack;
                var RA = context.Instruction.A + context.Frame.Base;
                stack.Get(RA) = context.TaskResult == 0 ? LuaValue.Nil : context.ResultsBuffer[0];
                stack.NotifyTop(RA + 1);
            };
        }

        if (isLen && vb.TryReadTable(out var table))
        {
            var RA = context.Instruction.A + context.Frame.Base;
            stack.Get(RA) = table.ArrayLength;
            stack.NotifyTop(RA + 1);
            return null;
        }
            
        LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), description, vb);
        return null;
    }
    
    static PostOperation? ExecuteCompareOperationMetaMethod(LuaValue vb,LuaValue vc,ref VirtualMachineExecutionContext context, string name,string? description)
    {
        if (vb.TryGetMetamethod(context.State, name, out var metamethod) || vc.TryGetMetamethod(context.State, name, out metamethod))
        {
            if (!metamethod.TryReadFunction(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var rootChunk = context.RootChunk;
            var state = context.State;
            var thread = context.Thread;
            var cancellationToken = context.CancellationToken;
            var stack = context.Stack;
            stack.Push(vb);
            stack.Push(vc);
            context.Pushing = true;
            context.Task = func.InvokeAsyncPushOnly(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = 2,
                FrameBase = stack.Count - 2,
                SourcePosition = context.Chunk.SourcePositions[context.Pc],
                ChunkName = context.Chunk.Name,
                RootChunkName = rootChunk.Name,
            }, context.ResultsBuffer.AsMemory(), cancellationToken);

            return static (ref VirtualMachineExecutionContext context) =>
            {
                var compareResult = context.ResultCount != 0 && context.ResultsBuffer[0].ToBoolean();
                if (compareResult != (context.Instruction.A == 1))
                {
                    context.Pc++;
                }
            };
        }
        
        if(description!=null)
        {
            LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), description, vb, vc);
        }
        else
        {
            if (context.Instruction.A == 1)
            {
                context.Pc++;
            }
        }
        return null;
    }

    static (int FrameBase, int ArgumentCount) PrepareForFunctionCall(LuaThread thread, LuaFunction function, Instruction instruction, int RA, bool isTailCall)
    {
        var stack = thread.Stack;

        var argumentCount = instruction.B - 1;
        if (argumentCount == -1)
        {
            argumentCount = (ushort)(stack.Count - (RA + 1));
        }

        var newBase = RA + 1;

        // In the case of tailcall, the local variables of the caller are immediately discarded, so there is no need to retain them.
        // Therefore, a call can be made without allocating new registers.
        if (isTailCall)
        {
            var currentBase = thread.GetCurrentFrame().Base;

            var stackBuffer = stack.GetBuffer();
            stackBuffer.Slice(newBase, argumentCount).CopyTo(stackBuffer.Slice(currentBase, argumentCount));
            newBase = currentBase;
        }

        var variableArgumentCount = function.GetVariableArgumentCount(argumentCount);

        // If there are variable arguments, the base of the stack is moved by that number and the values of the variable arguments are placed in front of it.
        // see: https://wubingzheng.github.io/build-lua-in-rust/en/ch08-02.arguments.html
        if (variableArgumentCount > 0)
        {
            var temp = newBase;
            newBase += variableArgumentCount;

            stack.EnsureCapacity(newBase + argumentCount);
            stack.NotifyTop(newBase + argumentCount);

            var stackBuffer = stack.GetBuffer()[temp..];
            stackBuffer[..argumentCount].CopyTo(stackBuffer[variableArgumentCount..]);
            stackBuffer.Slice(argumentCount, variableArgumentCount).CopyTo(stackBuffer);
        }

        return (newBase, argumentCount);
    }

    static Traceback GetTracebacks(ref VirtualMachineExecutionContext context)
    {
        return GetTracebacks(context.State, context.Chunk, context.Pc);
    }

    static Traceback GetTracebacks(LuaState state, Chunk chunk, int pc)
    {
        var frame = state.CurrentThread.GetCurrentFrame();
        state.CurrentThread.PushCallStackFrame(frame with
        {
            CallPosition = chunk.SourcePositions[pc],
            ChunkName = chunk.Name,
            RootChunkName = chunk.GetRoot().Name,
        });
        var tracebacks = state.GetTraceback();
        state.CurrentThread.PopCallStackFrame();

        return tracebacks;
    }
}