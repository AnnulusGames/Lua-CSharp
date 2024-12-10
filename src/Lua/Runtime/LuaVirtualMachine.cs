using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lua.Internal;

namespace Lua.Runtime;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
public static partial class LuaVirtualMachine
{
    [StructLayout(LayoutKind.Auto)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    struct VirtualMachineExecutionContext(
        LuaState state,
        LuaStack stack,
        LuaValue[] resultsBuffer,
        Memory<LuaValue> buffer,
        LuaThread thread,
        in CallStackFrame frame,
        CancellationToken cancellationToken)
    {
        public readonly LuaState State = state;
        public readonly LuaStack Stack = stack;
        public Closure Closure = (Closure)frame.Function;
        public readonly LuaValue[] ResultsBuffer = resultsBuffer;
        public readonly Memory<LuaValue> Buffer = buffer;
        public readonly LuaThread Thread = thread;
        public Chunk Chunk => Closure.Proto;
        public int FrameBase = frame.Base;
        public int VariableArgumentCount = frame.VariableArgumentCount;
        public readonly CancellationToken CancellationToken = cancellationToken;
        public int Pc = -1;
        public Instruction Instruction;
        public int ResultCount;
        public int TaskResult;
        public ValueTaskAwaiter<int> Awaiter;
        public bool IsTopLevel => BaseCallStackCount == Thread.CallStack.Count;

        readonly int BaseCallStackCount = thread.CallStack.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Pop(Instruction instruction, int frameBase)
        {
            if (BaseCallStackCount == Thread.CallStack.Count) return false;
            var count = instruction.B - 1;
            var src = instruction.A + frameBase;
            if (count == -1) count = Stack.Count - src;
            return PopFromBuffer(Stack.GetBuffer().Slice(src, count));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool PopFromBuffer(Span<LuaValue> result)
        {
            ref var callStack = ref Thread.CallStack;
            Re:
            var frames = callStack.AsSpan();
            if (frames.Length == BaseCallStackCount) return false;
            ref readonly var frame = ref frames[^1];
            Pc = frame.CallerInstructionIndex;
            ref readonly var lastFrame = ref frames[^2];
            Closure = Unsafe.As<Closure>(lastFrame.Function);
            var callInstruction = Chunk.Instructions[Pc];
            FrameBase = lastFrame.Base;
            VariableArgumentCount = lastFrame.VariableArgumentCount;
            if (callInstruction.OpCode == OpCode.TailCall)
            {
                Thread.PopCallStackFrameUnsafe();
                goto Re;
            }

            var opCode = callInstruction.OpCode;
            if (opCode is OpCode.Eq or OpCode.Lt or OpCode.Le)
            {
                var compareResult = result.Length > 0 && result[0].ToBoolean();
                if ((frame.Flags & CallStackFrameFlags.ReversedLe) != 0)
                {
                    compareResult = !compareResult;
                }

                if (compareResult != (callInstruction.A == 1))
                {
                    Pc++;
                }

                Thread.PopCallStackFrameUnsafe(frame.Base);
                return true;
            }

            var target = callInstruction.A + FrameBase;
            var targetCount = result.Length;
            switch (opCode)
            {
                case OpCode.Call:
                {
                    var c = callInstruction.C;
                    if (c != 0)
                    {
                        targetCount = c - 1;
                    }

                    break;
                }
                case OpCode.TForCall:
                    target += 3;
                    targetCount = callInstruction.C;
                    break;

                case OpCode.Self:
                    Stack.Get(target) = result.Length == 0 ? LuaValue.Nil : result[0];
                    Thread.PopCallStackFrameUnsafe(target + 2);
                    return true;
                case OpCode.SetTable or OpCode.SetTabUp:
                    targetCount = 0;
                    break;
                // Other opcodes has one result
                default:
                    targetCount = 1;
                    break;
            }

            var count = Math.Min(result.Length, targetCount);
            Stack.EnsureCapacity(target + targetCount);


            var stackBuffer = Stack.GetBuffer();
            if (count > 0)
            {
                result[..count].CopyTo(stackBuffer.Slice(target, count));
            }

            if (targetCount > count)
            {
                stackBuffer.Slice(target + count, targetCount - count).Clear();
            }

            Stack.NotifyTop(target + targetCount);
            Thread.PopCallStackFrameUnsafe(target + targetCount);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(in CallStackFrame frame)
        {
            Pc = -1;
            Closure = (frame.Function as Closure)!;
            FrameBase = frame.Base;
            VariableArgumentCount = frame.VariableArgumentCount;
        }

        public void PopOnTopCallStackFrames()
        {
            ref var callStack = ref Thread.CallStack;
            var count = callStack.Count;
            if (count == BaseCallStackCount) return;
            while (callStack.Count > BaseCallStackCount + 1)
            {
                callStack.TryPop();
            }

            Thread.PopCallStackFrame();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearResultsBuffer()
        {
            if (TaskResult == 0) return;
            if (TaskResult == 1)
            {
                ResultsBuffer[0] = default;
                return;
            }

            ResultsBuffer.AsSpan(0, TaskResult).Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearResultsBuffer(int count)
        {
            if (count == 0) return;
            if (count == 1)
            {
                ResultsBuffer[0] = default;
                return;
            }

            ResultsBuffer.AsSpan(0, count).Clear();
        }
    }

    enum PostOperationType
    {
        None,
        Nop,
        SetResult,
        TForCall,
        Call,
        TailCall,
        Self,
        Compare,
    }


    [AsyncStateMachine(typeof(AsyncStateMachine))]
    internal static ValueTask<int> ExecuteClosureAsync(LuaState luaState, Memory<LuaValue> buffer,
        CancellationToken cancellationToken)
    {
        var thread = luaState.CurrentThread;
        ref readonly var frame = ref thread.GetCallStackFrames()[^1];
        var resultBuffer = LuaValueArrayPool.Rent1024();

        var stateMachine = new AsyncStateMachine
        {
            Context = new(luaState, thread.Stack, resultBuffer, buffer, thread, in frame,
                cancellationToken),
            Builder = new()
        };
        stateMachine.Builder.Start(ref stateMachine);
        return stateMachine.Builder.Task;
    }

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
        PostOperationType postOperation;

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public void MoveNext()
        {
            //If the state is end, the function is done, so set the result and return. I think this state is not reachable in this implementation
            if (state == State.End)
            {
                Builder.SetResult(Context.ResultCount);
                return;
            }

            ref var context = ref Context;
            try
            {
                //If the state is State.Await, it means the task is awaited, so get the result and continue
                if (state == State.Await)
                {
                    context.TaskResult = context.Awaiter.GetResult();
                    context.Awaiter = default;
                    context.Thread.PopCallStackFrame();
                    switch (postOperation)
                    {
                        case PostOperationType.Nop: break;
                        case PostOperationType.SetResult:
                            var RA = context.Instruction.A + context.FrameBase;
                            context.Stack.Get(RA) = context.TaskResult == 0 ? LuaValue.Nil : context.ResultsBuffer[0];
                            context.Stack.NotifyTop(RA + 1);
                            context.ClearResultsBuffer();
                            break;
                        case PostOperationType.TForCall:
                            TForCallPostOperation(ref context);
                            break;
                        case PostOperationType.Call:
                            CallPostOperation(ref context);
                            break;
                        case PostOperationType.TailCall:
                            var resultsSpan = context.ResultsBuffer.AsSpan(0, context.TaskResult);
                            if (!context.PopFromBuffer(resultsSpan))
                            {
                                context.ResultCount = context.TaskResult;
                                resultsSpan.CopyTo(context.Buffer.Span);
                                state = State.End;
                                resultsSpan.Clear();
                                LuaValueArrayPool.Return1024(context.ResultsBuffer);
                                Builder.SetResult(context.TaskResult);
                                return;
                            }

                            resultsSpan.Clear();
                            break;
                        case PostOperationType.Self:
                            SelfPostOperation(ref context);
                            break;
                        case PostOperationType.Compare:
                            ComparePostOperation(ref context);
                            break;
                    }

                    postOperation = 0;
                    state = State.Running;
                }

                //This is a label to restart the execution when new function is called or restarted
                Restart:

                ref var instructionsHead = ref context.Chunk.Instructions[0];
                var frameBase = context.FrameBase;
                var stack = context.Stack;
                stack.EnsureCapacity(frameBase + context.Chunk.MaxStackPosition);
                ref var constHead = ref MemoryMarshalEx.UnsafeElementAt(context.Chunk.Constants, 0);

                while (true)
                {
                    var instructionRef = Unsafe.Add(ref instructionsHead, ++context.Pc);
                    context.Instruction = instructionRef;
                    switch (instructionRef.OpCode)
                    {
                        case OpCode.Move:
                            var instruction = instructionRef;
                            ref var stackHead = ref stack.FastGet(frameBase);
                            var iA = instruction.A;
                            Unsafe.Add(ref stackHead, iA) = Unsafe.Add(ref stackHead, instruction.UIntB);
                            stack.NotifyTop(iA + frameBase + 1);
                            continue;
                        case OpCode.LoadK:
                            instruction = instructionRef;
                            stack.GetWithNotifyTop(instruction.A + frameBase) = Unsafe.Add(ref constHead, instruction.Bx);
                            continue;
                        case OpCode.LoadBool:
                            instruction = instructionRef;
                            stack.GetWithNotifyTop(instruction.A + frameBase) = instruction.B != 0;
                            if (instruction.C != 0) context.Pc++;
                            continue;
                        case OpCode.LoadNil:
                            instruction = instructionRef;
                            var ra1 = instruction.A + frameBase + 1;
                            var iB = instruction.B;
                            stack.GetBuffer().Slice(ra1 - 1, iB + 1).Clear();
                            stack.NotifyTop(ra1 + iB);
                            continue;
                        case OpCode.GetUpVal:
                            instruction = instructionRef;
                            stack.GetWithNotifyTop(instruction.A + frameBase) = context.Closure.GetUpValue(instruction.B);
                            continue;
                        case OpCode.GetTabUp:
                            instruction = instructionRef;
                            stackHead = ref stack.FastGet(frameBase);
                            ref readonly var vc = ref RKC(ref stackHead, ref constHead, instruction);
                            var table = context.Closure.GetUpValue(instruction.B);

                            if (table.TryReadTable(out var luaTable) && luaTable.TryGetValue(vc, out var resultValue))
                            {
                                stack.GetWithNotifyTop(instruction.A + frameBase) = resultValue;
                                continue;
                            }

                            if (TryGetMetaTableValue(table, vc, ref context, out var doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.GetTable:
                            instruction = instructionRef;
                            stackHead = ref stack.FastGet(frameBase);
                            ref readonly var vb = ref Unsafe.Add(ref stackHead, instruction.UIntB);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (vb.TryReadTable(out luaTable) && luaTable.TryGetValue(vc, out resultValue))
                            {
                                stack.GetWithNotifyTop(instruction.A + frameBase) = resultValue;
                                continue;
                            }

                            if (TryGetMetaTableValue(vb, vc, ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.SetTabUp:
                            instruction = instructionRef;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            if (vb.TryReadNumber(out var numB))
                            {
                                if (double.IsNaN(numB))
                                {
                                    ThrowLuaRuntimeException(ref context, "table index is NaN");
                                    return;
                                }
                            }

                            table = context.Closure.GetUpValue(instruction.A);

                            if (table.TryReadTable(out luaTable))
                            {
                                luaTable[vb] = RKC(ref stackHead, ref constHead, instruction);
                                continue;
                            }

                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (TrySetMetaTableValue(table, vb, vc, ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Nop;
                            goto Await;

                        case OpCode.SetUpVal:
                            instruction = instructionRef;
                            context.Closure.SetUpValue(instruction.B, stack.FastGet(instruction.A + frameBase));
                            continue;
                        case OpCode.SetTable:
                            instruction = instructionRef;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            if (vb.TryReadNumber(out numB))
                            {
                                if (double.IsNaN(numB))
                                {
                                    ThrowLuaRuntimeException(ref context, " table index is NaN");
                                    return;
                                }
                            }

                            table = Unsafe.Add(ref stackHead, instruction.A);

                            if (table.TryReadTable(out luaTable))
                            {
                                if (luaTable.Metatable == null || !luaTable.Metatable.ContainsKey(Metamethods.NewIndex) || luaTable.ContainsKey(vb))
                                {
                                    luaTable[vb] = RKC(ref stackHead, ref constHead, instruction);
                                    continue;
                                }
                            }

                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (TrySetMetaTableValue(table, vb, vc, ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Nop;
                            goto Await;
                        case OpCode.NewTable:
                            instruction = instructionRef;
                            stack.GetWithNotifyTop(instruction.A + frameBase) = new LuaTable(instruction.B, instruction.C);
                            continue;
                        case OpCode.Self:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            table = Unsafe.Add(ref stackHead, instruction.UIntB);
                            if (table.TryReadTable(out luaTable) && luaTable.TryGetValue(vc, out resultValue))
                            {
                                Unsafe.Add(ref stackHead, iA) = resultValue;
                                Unsafe.Add(ref stackHead, iA + 1) = table;
                                stack.NotifyTop(iA + frameBase + 2);
                                continue;
                            }


                            if (TryGetMetaTableValue(table, vc, ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Self;
                            goto Await;
                        case OpCode.Add:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (vb.Type == LuaValueType.Number && vc.Type == LuaValueType.Number)
                            {
                                Unsafe.Add(ref stackHead, iA) = vb.UnsafeReadDouble() + vc.UnsafeReadDouble();
                                stack.NotifyTop(iA + frameBase + 1);
                                continue;
                            }

                            if (vb.TryReadDouble(out numB) && vc.TryReadDouble(out var numC))
                            {
                                Unsafe.Add(ref stackHead, iA) = numB + numC;
                                stack.NotifyTop(iA + frameBase + 1);
                                continue;
                            }

                            if (ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Add, "add", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Sub:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);

                            if (vb.Type == LuaValueType.Number && vc.Type == LuaValueType.Number)
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = vb.UnsafeReadDouble() - vc.UnsafeReadDouble();
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (vb.TryReadDouble(out numB) && vc.TryReadDouble(out numC))
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = numB - numC;
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Sub, "sub", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;

                        case OpCode.Mul:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);

                            if (vb.Type == LuaValueType.Number && vc.Type == LuaValueType.Number)
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = vb.UnsafeReadDouble() * vc.UnsafeReadDouble();
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (vb.TryReadDouble(out numB) && vc.TryReadDouble(out numC))
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = numB * numC;
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Mul, "mul", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;

                        case OpCode.Div:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);

                            if (vb.Type == LuaValueType.Number && vc.Type == LuaValueType.Number)
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = vb.UnsafeReadDouble() / vc.UnsafeReadDouble();
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (vb.TryReadDouble(out numB) && vc.TryReadDouble(out numC))
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = numB / numC;
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Div, "div", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Mod:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (vb.TryReadDouble(out numB) && vc.TryReadDouble(out numC))
                            {
                                var mod = numB % numC;
                                if ((numC > 0 && mod < 0) || (numC < 0 && mod > 0))
                                {
                                    mod += numC;
                                }

                                Unsafe.Add(ref stackHead, iA) = mod;
                                continue;
                            }

                            if (ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Mod, "mod", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Pow:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (vb.TryReadDouble(out numB) && vc.TryReadDouble(out numC))
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = Math.Pow(numB, numC);
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Pow, "pow", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Unm:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.FastGet(frameBase);
                            vb = ref Unsafe.Add(ref stackHead, instruction.UIntB);

                            if (vb.TryReadDouble(out numB))
                            {
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = -numB;
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (ExecuteUnaryOperationMetaMethod(vb, ref context, Metamethods.Unm, "unm", false, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Not:
                            instruction = instructionRef;
                            iA = instruction.A;
                            ra1 = iA + frameBase + 1;
                            stackHead = ref stack.FastGet(frameBase);
                            Unsafe.Add(ref stackHead, iA) = !Unsafe.Add(ref stackHead, instruction.UIntB).ToBoolean();
                            stack.NotifyTop(ra1);
                            continue;

                        case OpCode.Len:
                            instruction = instructionRef;
                            stackHead = ref stack.FastGet(frameBase);

                            vb = ref Unsafe.Add(ref stackHead, instruction.UIntB);

                            if (vb.TryReadString(out var str))
                            {
                                iA = instruction.A;
                                ra1 = iA + frameBase + 1;
                                Unsafe.Add(ref stackHead, iA) = str.Length;
                                stack.NotifyTop(ra1);
                                continue;
                            }

                            if (ExecuteUnaryOperationMetaMethod(vb, ref context, Metamethods.Len, "get length of", true, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Concat:
                            if (Concat(ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.SetResult;
                            goto Await;
                        case OpCode.Jmp:
                            instruction = instructionRef;
                            context.Pc += instruction.SBx;
                            iA = instruction.A;
                            if (iA != 0)
                            {
                                context.State.CloseUpValues(context.Thread, frameBase + iA - 1);
                            }

                            continue;
                        case OpCode.Eq:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.Get(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);
                            if (vb == vc)
                            {
                                if (iA != 1)
                                {
                                    context.Pc++;
                                }

                                continue;
                            }

                            if (ExecuteCompareOperationMetaMethod(vb, vc, ref context, Metamethods.Eq, null, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Compare;
                            goto Await;
                        case OpCode.Lt:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.Get(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);

                            if (vb.TryReadNumber(out numB) && vc.TryReadNumber(out numC))
                            {
                                var compareResult = numB < numC;
                                if (compareResult != (iA == 1))
                                {
                                    context.Pc++;
                                }

                                continue;
                            }


                            if (vb.TryReadString(out var strB) && vc.TryReadString(out var strC))
                            {
                                var compareResult = StringComparer.Ordinal.Compare(strB, strC) < 0;
                                if (compareResult != (iA == 1))
                                {
                                    context.Pc++;
                                }

                                continue;
                            }

                            if (ExecuteCompareOperationMetaMethod(vb, vc, ref context, Metamethods.Lt, "less than", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Compare;
                            goto Await;

                        case OpCode.Le:
                            instruction = instructionRef;
                            iA = instruction.A;
                            stackHead = ref stack.Get(frameBase);
                            vb = ref RKB(ref stackHead, ref constHead, instruction);
                            vc = ref RKC(ref stackHead, ref constHead, instruction);

                            if (vb.TryReadNumber(out numB) && vc.TryReadNumber(out numC))
                            {
                                var compareResult = numB <= numC;
                                if (compareResult != (iA == 1))
                                {
                                    context.Pc++;
                                }

                                continue;
                            }

                            if (vb.TryReadString(out strB) && vc.TryReadString(out strC))
                            {
                                var compareResult = StringComparer.Ordinal.Compare(strB, strC) <= 0;
                                if (compareResult != (iA == 1))
                                {
                                    context.Pc++;
                                }

                                continue;
                            }

                            if (ExecuteCompareOperationMetaMethod(vb, vc, ref context, Metamethods.Le, "less than or equals", out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Compare;
                            goto Await;

                        case OpCode.Test:
                            instruction = instructionRef;
                            if (stack.Get(instruction.A + frameBase).ToBoolean() != (instruction.C == 1))
                            {
                                context.Pc++;
                            }

                            continue;
                        case OpCode.TestSet:
                            instruction = instructionRef;
                            vb = ref stack.Get(instruction.B + frameBase);
                            if (vb.ToBoolean() != (instruction.C == 1))
                            {
                                context.Pc++;
                            }
                            else
                            {
                                stack.GetWithNotifyTop(instruction.A + frameBase) = vb;
                            }

                            continue;

                        case OpCode.Call:
                            if (Call(ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.Call;
                            goto Await;
                        case OpCode.TailCall:
                            if (TailCall(ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                if (context.IsTopLevel) goto End;
                                continue;
                            }

                            postOperation = PostOperationType.TailCall;
                            goto Await;
                        case OpCode.Return:
                            instruction = instructionRef;
                            iA = instruction.A;
                            ra1 = iA + frameBase + 1;
                            context.State.CloseUpValues(context.Thread, frameBase);
                            if (context.Pop(instruction, frameBase)) goto Restart;
                            var retCount = instruction.B - 1;

                            if (retCount == -1)
                            {
                                retCount = stack.Count - (ra1 - 1);
                            }

                            if (0 < retCount)
                            {
                                stack.GetBuffer().Slice(ra1 - 1, retCount).CopyTo(context.Buffer.Span);
                            }

                            context.ResultCount = retCount;
                            goto End;
                        case OpCode.ForLoop:
                            ref var indexRef = ref stack.Get(instructionRef.A + frameBase);
                            var limit = Unsafe.Add(ref indexRef, 1).UnsafeReadDouble();
                            var step = Unsafe.Add(ref indexRef, 2).UnsafeReadDouble();
                            var index = indexRef.UnsafeReadDouble() + step;

                            if (step >= 0 ? index <= limit : limit <= index)
                            {
                                context.Pc += instructionRef.SBx;
                                indexRef = index;
                                Unsafe.Add(ref indexRef, 3) = index;
                                stack.NotifyTop(instructionRef.A + frameBase + 4);
                                continue;
                            }

                            stack.NotifyTop(instructionRef.A + frameBase + 1);
                            continue;
                        case OpCode.ForPrep:
                            indexRef = ref stack.Get(instructionRef.A + frameBase);

                            if (!indexRef.TryReadDouble(out var init))
                            {
                                ThrowLuaRuntimeException(ref context, "'for' initial value must be a number");
                                return;
                            }

                            if (!LuaValue.TryReadOrSetDouble(ref Unsafe.Add(ref indexRef, 1), out _))
                            {
                                ThrowLuaRuntimeException(ref context, "'for' limit must be a number");
                                return;
                            }

                            if (!LuaValue.TryReadOrSetDouble(ref Unsafe.Add(ref indexRef, 2), out step))
                            {
                                ThrowLuaRuntimeException(ref context, "'for' step must be a number");
                                return;
                            }

                            indexRef = init - step;
                            stack.NotifyTop(instructionRef.A + frameBase + 1);
                            context.Pc += instructionRef.SBx;
                            continue;
                        case OpCode.TForCall:
                            if (TForCall(ref context, out doRestart))
                            {
                                if (doRestart) goto Restart;
                                continue;
                            }

                            postOperation = PostOperationType.TForCall;
                            goto Await;
                        case OpCode.TForLoop:
                            instruction = instructionRef;
                            iA = instruction.A;
                            ra1 = iA + frameBase + 1;
                            ref var forState = ref stack.Get(ra1);

                            if (forState.Type is not LuaValueType.Nil)
                            {
                                Unsafe.Add(ref forState, -1) = forState;
                                context.Pc += instruction.SBx;
                            }

                            continue;
                        case OpCode.SetList:
                            SetList(ref context);
                            continue;
                        case OpCode.Closure:
                            instruction = instructionRef;
                            iA = instruction.A;
                            ra1 = iA + frameBase + 1;
                            stack.EnsureCapacity(ra1);
                            stack.Get(ra1 - 1) = new Closure(context.State, context.Chunk.Functions[instruction.SBx]);
                            stack.NotifyTop(ra1);
                            continue;
                        case OpCode.VarArg:
                            instruction = instructionRef;
                            iA = instruction.A;
                            ra1 = iA + frameBase + 1;
                            var frameVariableArgumentCount = context.VariableArgumentCount;
                            var count = instruction.B == 0
                                ? frameVariableArgumentCount
                                : instruction.B - 1;
                            var ra = ra1 - 1;
                            stack.EnsureCapacity(ra + count);
                            stackHead = ref stack.Get(0);
                            for (int i = 0; i < count; i++)
                            {
                                Unsafe.Add(ref stackHead, ra + i) = frameVariableArgumentCount > i
                                    ? Unsafe.Add(ref stackHead, frameBase - (frameVariableArgumentCount - i))
                                    : default;
                            }

                            stack.NotifyTop(ra + count);
                            continue;
                        case OpCode.ExtraArg:
                        default:
                            ThrowLuaNotImplementedException(ref context, context.Instruction.OpCode);
                            return;
                    }
                }

                Await:
                //Set the state to await and return with setting this method as the task's continuation
                state = State.Await;
                Builder.AwaitOnCompleted(ref context.Awaiter, ref this);
                return;


                End:
                state = State.End;
                LuaValueArrayPool.Return1024(context.ResultsBuffer);
                Builder.SetResult(context.ResultCount);
            }
            catch (Exception e)
            {
                if (e is not LuaRuntimeException)
                {
                    e = new LuaRuntimeCSharpException(GetTracebacks(ref context), e);
                }

                context.PopOnTopCallStackFrames();
                context.State.CloseUpValues(context.Thread, context.FrameBase);
                LuaValueArrayPool.Return1024(context.ResultsBuffer, true);
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

        static void ThrowLuaRuntimeException(ref VirtualMachineExecutionContext context, string message)
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), message);
        }

        static void ThrowLuaNotImplementedException(ref VirtualMachineExecutionContext context, OpCode opcode)
        {
            throw new LuaRuntimeException(context.State.GetTraceback(), $"OpCode {opcode} is not implemented");
        }
    }


    static void SelfPostOperation(ref VirtualMachineExecutionContext context)
    {
        var stack = context.Stack;
        var instruction = context.Instruction;
        var RA = instruction.A + context.FrameBase;
        var RB = instruction.B + context.FrameBase;
        ref var stackHead = ref stack.Get(0);
        var table = Unsafe.Add(ref stackHead, RB);
        Unsafe.Add(ref stackHead, RA + 1) = table;
        Unsafe.Add(ref stackHead, RA) = context.TaskResult == 0 ? LuaValue.Nil : context.ResultsBuffer[0];
        stack.NotifyTop(RA + 2);
        context.ClearResultsBuffer();
    }

    static bool Concat(ref VirtualMachineExecutionContext context, out bool doRestart)
    {
        var instruction = context.Instruction;
        var stack = context.Stack;
        var RA = instruction.A + context.FrameBase;
        stack.EnsureCapacity(RA + 1);
        ref var stackHead = ref stack.Get(context.FrameBase);
        ref var constHead = ref MemoryMarshalEx.UnsafeElementAt(context.Chunk.Constants, 0);
        var vb = RKB(ref stackHead, ref constHead, instruction);
        var vc = RKC(ref stackHead, ref constHead, instruction);

        var bIsValid = vb.TryReadString(out var strB);
        var cIsValid = vc.TryReadString(out var strC);

        if (!bIsValid && vb.TryReadDouble(out var numB))
        {
            strB = numB.ToString(CultureInfo.InvariantCulture);
            bIsValid = true;
        }

        if (!cIsValid && vc.TryReadDouble(out var numC))
        {
            strC = numC.ToString(CultureInfo.InvariantCulture);
            cIsValid = true;
        }

        if (bIsValid && cIsValid)
        {
            stack.Get(RA) = strB + strC;
            stack.NotifyTop(RA + 1);
            doRestart = false;
            return true;
        }

        return ExecuteBinaryOperationMetaMethod(vb, vc, ref context, Metamethods.Concat, "concat", out doRestart);
    }

    static bool Call(ref VirtualMachineExecutionContext context, out bool doRestart)
    {
        var instruction = context.Instruction;
        var RA = instruction.A + context.FrameBase;
        var va = context.Stack.Get(RA);
        if (!va.TryReadFunction(out var func))
        {
            if (va.TryGetMetamethod(context.State, Metamethods.Call, out var metamethod) &&
                metamethod.TryReadFunction(out func))
            {
            }
            else
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", va);
            }
        }


        var thread = context.Thread;
        var (newBase, argumentCount, variableArgumentCount) = PrepareForFunctionCall(thread, func, instruction, RA);

        var newFrame = func.CreateNewFrame(ref context, newBase, variableArgumentCount);

        thread.PushCallStackFrame(newFrame);
        if (func.IsClosure)
        {
            context.Push(newFrame);
            doRestart = true;
            return true;
        }

        doRestart = false;
        return FuncCall(ref context, in newFrame, func, newBase, argumentCount);

        static bool FuncCall(ref VirtualMachineExecutionContext context, in CallStackFrame newFrame, LuaFunction func, int newBase, int argumentCount)
        {
            {
                var task = func.Invoke(ref context, newFrame, argumentCount);

                var awaiter = task.GetAwaiter();
                if (!awaiter.IsCompleted)
                {
                    context.Awaiter = awaiter;
                    return false;
                }

                context.Thread.PopCallStackFrameUnsafe(newBase);
                context.TaskResult = awaiter.GetResult();
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
                    var RA = instruction.A + context.FrameBase;
                    stack.EnsureCapacity(RA + resultCount);
                    ref var stackHead = ref stack.Get(RA);
                    var results = context.ResultsBuffer.AsSpan(0, rawResultCount);
                    for (int i = 0; i < resultCount; i++)
                    {
                        Unsafe.Add(ref stackHead, i) = i >= rawResultCount
                            ? default
                            : results[i];
                    }

                    stack.NotifyTop(RA + resultCount);
                    results.Clear();
                }

                return true;
            }
        }
    }

    static void CallPostOperation(ref VirtualMachineExecutionContext context)
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
            var RA = instruction.A + context.FrameBase;
            stack.EnsureCapacity(RA + resultCount);
            ref var stackHead = ref stack.Get(RA);
            var results = context.ResultsBuffer.AsSpan(0, rawResultCount);
            for (int i = 0; i < resultCount; i++)
            {
                Unsafe.Add(ref stackHead, i) = i >= rawResultCount
                    ? default
                    : results[i];
            }

            stack.NotifyTop(RA + resultCount);
            results.Clear();
        }
    }

    static bool TailCall(ref VirtualMachineExecutionContext context, out bool doRestart)
    {
        var instruction = context.Instruction;
        var stack = context.Stack;
        var RA = instruction.A + context.FrameBase;
        var state = context.State;
        var thread = context.Thread;

        state.CloseUpValues(thread, context.FrameBase);

        var va = stack.Get(RA);
        if (!va.TryReadFunction(out var func))
        {
            if (!va.TryGetMetamethod(state, Metamethods.Call, out var metamethod) &&
                !metamethod.TryReadFunction(out func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }
        }

        var (newBase, argumentCount, variableArgumentCount) = PrepareForFunctionTailCall(thread, func, instruction, RA);

        var newFrame = func.CreateNewFrame(ref context, newBase, variableArgumentCount);
        thread.PushCallStackFrame(newFrame);

        context.Push(newFrame);
        if (func is Closure)
        {
            doRestart = true;
            return true;
        }

        doRestart = false;
        var task = func.Invoke(ref context, newFrame, argumentCount);


        var awaiter = task.GetAwaiter();
        if (!awaiter.IsCompleted)
        {
            context.Awaiter = awaiter;
            return false;
        }

        context.Thread.PopCallStackFrame();

        doRestart = true;
        var resultCount = awaiter.GetResult();
        var resultsSpan = context.ResultsBuffer.AsSpan(0, resultCount);
        if (!context.PopFromBuffer(resultsSpan))
        {
            doRestart = false;
            context.ResultCount = resultCount;
            resultsSpan.CopyTo(context.Buffer.Span);
        }

        resultsSpan.Clear();
        return true;
    }

    static bool TForCall(ref VirtualMachineExecutionContext context, out bool doRestart)
    {
        doRestart = false;
        var instruction = context.Instruction;
        var stack = context.Stack;
        var RA = instruction.A + context.FrameBase;

        var iteratorRaw = stack.Get(RA);
        if (!iteratorRaw.TryReadFunction(out var iterator))
        {
            LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", iteratorRaw);
        }

        var newBase = RA + 3 + instruction.C;
        stack.Get(newBase) = stack.Get(RA + 1);
        stack.Get(newBase + 1) = stack.Get(RA + 2);
        stack.NotifyTop(newBase + 2);
        var newFrame = iterator.CreateNewFrame(ref context, newBase);
        context.Thread.PushCallStackFrame(newFrame);
        if (iterator.IsClosure)
        {
            context.Push(newFrame);
            doRestart = true;
            return true;
        }

        var task = iterator.Invoke(ref context, newFrame, 2);

        var awaiter = task.GetAwaiter();
        if (!awaiter.IsCompleted)
        {
            context.Awaiter = awaiter;

            return false;
        }

        context.TaskResult = awaiter.GetResult();
        context.Thread.PopCallStackFrame();
        TForCallPostOperation(ref context);
        return true;
    }

    static void TForCallPostOperation(ref VirtualMachineExecutionContext context)
    {
        var stack = context.Stack;
        var instruction = context.Instruction;
        var RA = instruction.A + context.FrameBase;
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
        context.ClearResultsBuffer(resultCount);
    }

    static void SetList(ref VirtualMachineExecutionContext context)
    {
        var instruction = context.Instruction;
        var stack = context.Stack;
        var RA = instruction.A + context.FrameBase;

        if (!stack.Get(RA).TryReadTable(out var table))
        {
            throw new LuaException("internal error");
        }

        var count = instruction.B == 0
            ? stack.Count - (RA + 1)
            : instruction.B;

        table.EnsureArrayCapacity((instruction.C - 1) * 50 + count);
        stack.GetBuffer().Slice(RA + 1, count)
            .CopyTo(table.GetArraySpan()[((instruction.C - 1) * 50)..]);
    }

    static void ComparePostOperation(ref VirtualMachineExecutionContext context)
    {
        var compareResult = context.TaskResult != 0 && context.ResultsBuffer[0].ToBoolean();
        if (compareResult != (context.Instruction.A == 1))
        {
            context.Pc++;
        }

        context.ClearResultsBuffer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ref readonly LuaValue RKB(ref LuaValue stack, ref LuaValue constants, Instruction instruction)
    {
        var index = instruction.UIntB;
        return ref (index >= 256 ? ref Unsafe.Add(ref constants, index - 256) : ref Unsafe.Add(ref stack, index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ref readonly LuaValue RKC(ref LuaValue stack, ref LuaValue constants, Instruction instruction)
    {
        var index = instruction.UIntC;
        return ref (index >= 256 ? ref Unsafe.Add(ref constants, index - 256) : ref Unsafe.Add(ref stack, index));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TryGetMetaTableValue(LuaValue table, LuaValue key, ref VirtualMachineExecutionContext context, out bool doRestart)
    {
        var isSelf = context.Instruction.OpCode == OpCode.Self;
        doRestart = false;
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


            var newFrame = indexTable.CreateNewFrame(ref context, stack.Count - 2);

            context.Thread.PushCallStackFrame(newFrame);

            if (indexTable.IsClosure)
            {
                context.Push(newFrame);
                doRestart = true;
                return true;
            }

            var task = indexTable.Invoke(ref context, newFrame, 2);
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                context.Thread.PopCallStackFrame();
                var ra = context.Instruction.A + context.FrameBase;
                var resultCount = awaiter.GetResult();
                context.Stack.Get(ra) = resultCount == 0 ? default : context.ResultsBuffer[0];
                if (isSelf)
                {
                    context.Stack.Get(ra + 1) = table;
                    context.Stack.NotifyTop(ra + 2);
                }
                else
                {
                    context.Stack.NotifyTop(ra + 1);
                }

                context.ClearResultsBuffer(resultCount);
                return true;
            }

            context.Awaiter = awaiter;
            return false;
        }

        if (table.Type == LuaValueType.Table)
        {
            var ra = context.Instruction.A + context.FrameBase;
            context.Stack.Get(ra) = default;
            if (isSelf)
            {
                context.Stack.Get(ra + 1) = table;
                context.Stack.NotifyTop(ra + 2);
            }
            else
            {
                context.Stack.NotifyTop(ra + 1);
            }

            return true;
        }


        LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "index", table);
        return false;
    }

    static bool TrySetMetaTableValue(LuaValue table, LuaValue key, LuaValue value,
        ref VirtualMachineExecutionContext context, out bool doRestart)
    {
        doRestart = false;
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
            var newFrame = indexTable.CreateNewFrame(ref context, stack.Count - 3);

            context.Thread.PushCallStackFrame(newFrame);

            if (indexTable.IsClosure)
            {
                context.Push(newFrame);
                doRestart = true;
                return true;
            }

            var task = indexTable.Invoke(ref context, newFrame, 3);
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                thread.PopCallStackFrame();
                return true;
            }

            context.Awaiter = awaiter;
            return false;
        }

        LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "index", table);
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExecuteBinaryOperationMetaMethod(LuaValue vb, LuaValue vc,
        ref VirtualMachineExecutionContext context, string name, string description, out bool doRestart)
    {
        doRestart = false;
        if (vb.TryGetMetamethod(context.State, name, out var metamethod) ||
            vc.TryGetMetamethod(context.State, name, out metamethod))
        {
            if (!metamethod.TryReadFunction(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var stack = context.Stack;
            stack.Push(vb);
            stack.Push(vc);

            var newFrame = func.CreateNewFrame(ref context, stack.Count - 2);

            context.Thread.PushCallStackFrame(newFrame);

            if (func.IsClosure)
            {
                context.Push(newFrame);
                doRestart = true;
                return true;
            }


            var task = func.Invoke(ref context, newFrame, 2);
            context.Awaiter = task.GetAwaiter();

            if (context.Awaiter.IsCompleted)
            {
                var resultCount = context.Awaiter.GetResult();
                context.Thread.PopCallStackFrame();
                var RA = context.Instruction.A + context.FrameBase;
                stack.Get(RA) = resultCount == 0 ? LuaValue.Nil : context.ResultsBuffer[0];
                context.ClearResultsBuffer(resultCount);
                return true;
            }

            return false;
        }

        LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), description, vb, vc);
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExecuteUnaryOperationMetaMethod(LuaValue vb, ref VirtualMachineExecutionContext context,
        string name, string description, bool isLen, out bool doRestart)
    {
        doRestart = false;
        var stack = context.Stack;
        if (vb.TryGetMetamethod(context.State, name, out var metamethod))
        {
            if (!metamethod.TryReadFunction(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            stack.Push(vb);
            var newFrame = func.CreateNewFrame(ref context, stack.Count - 1);

            context.Thread.PushCallStackFrame(newFrame);

            if (func.IsClosure)
            {
                context.Push(newFrame);
                doRestart = true;
                return true;
            }


            var task = func.Invoke(ref context, newFrame, 1);

            context.Awaiter = task.GetAwaiter();
            if (context.Awaiter.IsCompleted)
            {
                context.Thread.PopCallStackFrame();
                var RA = context.Instruction.A + context.FrameBase;
                var resultCount = context.Awaiter.GetResult();
                stack.Get(RA) = resultCount == 0 ? LuaValue.Nil : context.ResultsBuffer[0];
                context.ClearResultsBuffer(resultCount);
                return true;
            }

            return false;
        }

        if (isLen && vb.TryReadTable(out var table))
        {
            var RA = context.Instruction.A + context.FrameBase;
            stack.Get(RA) = table.ArrayLength;
            return true;
        }

        LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), description, vb);
        return true;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ExecuteCompareOperationMetaMethod(LuaValue vb, LuaValue vc,
        ref VirtualMachineExecutionContext context, string name, string? description, out bool doRestart)
    {
        doRestart = false;
        bool reverseLe = false;
        ReCheck:
        if (vb.TryGetMetamethod(context.State, name, out var metamethod) ||
            vc.TryGetMetamethod(context.State, name, out metamethod))
        {
            if (!metamethod.TryReadFunction(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), "call", metamethod);
            }

            var stack = context.Stack;
            stack.Push(vb);
            stack.Push(vc);
            var newFrame = func.CreateNewFrame(ref context, stack.Count - 2);
            if (reverseLe) newFrame.Flags |= CallStackFrameFlags.ReversedLe;
            context.Thread.PushCallStackFrame(newFrame);

            if (func.IsClosure)
            {
                context.Push(newFrame);
                doRestart = true;
                return true;
            }

            var task = func.Invoke(ref context, newFrame, 2);
            context.Awaiter = task.GetAwaiter();

            if (context.Awaiter.IsCompleted)
            {
                context.Thread.PopCallStackFrame();
                var resultCount = context.Awaiter.GetResult();
                var compareResult = resultCount != 0 && context.ResultsBuffer[0].ToBoolean();
                compareResult = reverseLe ? !compareResult : compareResult;
                if (compareResult != (context.Instruction.A == 1))
                {
                    context.Pc++;
                }

                context.ClearResultsBuffer(resultCount);

                return true;
            }

            return false;
        }

        if (name == Metamethods.Le)
        {
            reverseLe = true;
            name = Metamethods.Lt;
            (vb, vc) = (vc, vb);
            goto ReCheck;
        }

        if (description != null)
        {
            if (reverseLe)
            {
                (vb, vc) = (vc, vb);
            }

            LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(ref context), description, vb, vc);
        }
        else
        {
            if (context.Instruction.A == 1)
            {
                context.Pc++;
            }
        }

        return true;
    }

    // If there are variable arguments, the base of the stack is moved by that number and the values of the variable arguments are placed in front of it.
    // see: https://wubingzheng.github.io/build-lua-in-rust/en/ch08-02.arguments.html
    [MethodImpl(MethodImplOptions.NoInlining)]
    static (int FrameBase, int ArgumentCount, int VariableArgumentCount) PrepareVariableArgument(LuaStack stack, int newBase, int argumentCount,
        int variableArgumentCount)
    {
        var temp = newBase;
        newBase += variableArgumentCount;

        stack.EnsureCapacity(newBase + argumentCount);
        stack.NotifyTop(newBase + argumentCount);

        var stackBuffer = stack.GetBuffer()[temp..];
        stackBuffer[..argumentCount].CopyTo(stackBuffer[variableArgumentCount..]);
        stackBuffer.Slice(argumentCount, variableArgumentCount).CopyTo(stackBuffer);
        return (newBase, argumentCount, variableArgumentCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static (int FrameBase, int ArgumentCount, int VariableArgumentCount) PrepareForFunctionCall(LuaThread thread, LuaFunction function,
        Instruction instruction, int RA)
    {
        var argumentCount = instruction.B - 1;
        if (argumentCount == -1)
        {
            argumentCount = (ushort)(thread.Stack.Count - (RA + 1));
        }
        else
        {
            thread.Stack.NotifyTop(RA + 1 + argumentCount);
        }

        var newBase = RA + 1;
        var variableArgumentCount = function.GetVariableArgumentCount(argumentCount);

        if (variableArgumentCount <= 0)
        {
            return (newBase, argumentCount, 0);
        }

        return PrepareVariableArgument(thread.Stack, newBase, argumentCount, variableArgumentCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static (int FrameBase, int ArgumentCount, int VariableArgumentCount) PrepareForFunctionTailCall(LuaThread thread, LuaFunction function,
        Instruction instruction, int RA)
    {
        var stack = thread.Stack;

        var argumentCount = instruction.B - 1;
        if (instruction.B == 0)
        {
            argumentCount = (ushort)(stack.Count - (RA + 1));
        }
        else
        {
            thread.Stack.NotifyTop(RA + 1 + argumentCount);
        }

        var newBase = RA + 1;

        // In the case of tailcall, the local variables of the caller are immediately discarded, so there is no need to retain them.
        // Therefore, a call can be made without allocating new registers.
        var currentBase = thread.GetCurrentFrame().Base;
        {
            var stackBuffer = stack.GetBuffer();
            if (argumentCount > 0)
                stackBuffer.Slice(newBase, argumentCount).CopyTo(stackBuffer.Slice(currentBase, argumentCount));
            newBase = currentBase;
        }

        var variableArgumentCount = function.GetVariableArgumentCount(argumentCount);

        if (variableArgumentCount <= 0)
        {
            return (newBase, argumentCount, 0);
        }

        return PrepareVariableArgument(thread.Stack, newBase, argumentCount, variableArgumentCount);
    }

    static Traceback GetTracebacks(ref VirtualMachineExecutionContext context)
    {
        return GetTracebacks(context.State, context.Pc);
    }

    static Traceback GetTracebacks(LuaState state, int pc)
    {
        var frame = state.CurrentThread.GetCurrentFrame();
        state.CurrentThread.PushCallStackFrame(frame with
        {
            CallerInstructionIndex = pc
        });
        var tracebacks = state.GetTraceback();
        state.CurrentThread.PopCallStackFrame();
        return tracebacks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static CallStackFrame CreateNewFrame(this LuaFunction function, ref VirtualMachineExecutionContext context, int newBase, int variableArgumentCount = 0)
    {
        return new()
        {
            Base = newBase,
            Function = function,
            VariableArgumentCount = variableArgumentCount,
            CallerInstructionIndex = context.Pc,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ValueTask<int> Invoke(this LuaFunction function, ref VirtualMachineExecutionContext context, in CallStackFrame frame, int arguments)
    {
        return function.Func(new()
        {
            State = context.State,
            Thread = context.Thread,
            ArgumentCount = arguments,
            FrameBase = frame.Base,
            CallerInstructionIndex = frame.CallerInstructionIndex,
        }, context.ResultsBuffer, context.CancellationToken);
    }
}