using System.Buffers;
using System.Runtime.CompilerServices;
using Lua.Internal;

namespace Lua.Runtime;

public static partial class LuaVirtualMachine
{
    internal async static ValueTask<int> ExecuteClosureAsync(LuaState state, Closure closure, CallStackFrame frame, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var thread = state.CurrentThread;
        var stack = thread.Stack;
        var chunk = closure.Proto;
        var rootChunk = chunk.GetRoot();

        var resultBuffer = ArrayPool<LuaValue>.Shared.Rent(1024);

        try
        {
            for (var pc = 0; pc < chunk.Instructions.Length; pc++)
            {
                var instruction = chunk.Instructions[pc];

                var RA = instruction.A + frame.Base;
                var RB = instruction.B + frame.Base;

                switch (instruction.OpCode)
                {
                    case OpCode.Move:
                        stack.EnsureCapacity(RA + 1);
                        stack.UnsafeGet(RA) = stack.UnsafeGet(RB);
                        stack.NotifyTop(RA + 1);
                        break;
                    case OpCode.LoadK:
                        stack.EnsureCapacity(RA + 1);
                        stack.UnsafeGet(RA) = chunk.Constants[instruction.Bx];
                        stack.NotifyTop(RA + 1);
                        break;
                    case OpCode.LoadKX:
                        throw new NotImplementedException();
                    case OpCode.LoadBool:
                        stack.EnsureCapacity(RA + 1);
                        stack.UnsafeGet(RA) = instruction.B != 0;
                        stack.NotifyTop(RA + 1);
                        if (instruction.C != 0) pc++;
                        break;
                    case OpCode.LoadNil:
                        stack.EnsureCapacity(RA + instruction.B + 1);
                        stack.GetBuffer().Slice(RA, instruction.B + 1).Clear();
                        stack.NotifyTop(RA + instruction.B + 1);
                        break;
                    case OpCode.GetUpVal:
                        {
                            stack.EnsureCapacity(RA + 1);
                            var upValue = closure.UpValues[instruction.B];
                            stack.UnsafeGet(RA) = upValue.GetValue();
                            stack.NotifyTop(RA + 1);
                            break;
                        }
                    case OpCode.GetTabUp:
                        {
                            stack.EnsureCapacity(RA + 1);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);
                            var upValue = closure.UpValues[instruction.B];
                            var table = upValue.GetValue();
                            await GetTableValue(state, thread, chunk, rootChunk, pc, table, vc, resultBuffer.AsMemory(), cancellationToken);
                            var value = resultBuffer[0];
                            stack.UnsafeGet(RA) = value;
                            stack.NotifyTop(RA + 1);
                            break;
                        }
                    case OpCode.GetTable:
                        {
                            stack.EnsureCapacity(RA + 1);
                            var table = stack.UnsafeGet(RB);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);
                            await GetTableValue(state, thread, chunk, rootChunk, pc, table, vc, resultBuffer.AsMemory(), cancellationToken);
                            var value = resultBuffer[0];
                            stack.UnsafeGet(RA) = value;
                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.SetTabUp:
                        {
                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            var upValue = closure.UpValues[instruction.A];
                            var table = upValue.GetValue();
                            await SetTableValue(state, thread, chunk, rootChunk, pc, table, vb, vc, resultBuffer.AsMemory(), cancellationToken);
                            break;
                        }
                    case OpCode.SetUpVal:
                        {
                            var upValue = closure.UpValues[instruction.B];
                            upValue.SetValue(stack.UnsafeGet(RA));
                            break;
                        }
                    case OpCode.SetTable:
                        {
                            var table = stack.UnsafeGet(RA);
                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);
                            await SetTableValue(state, thread, chunk, rootChunk, pc, table, vb, vc, resultBuffer.AsMemory(), cancellationToken);
                        }
                        break;
                    case OpCode.NewTable:
                        stack.EnsureCapacity(RA + 1);
                        stack.UnsafeGet(RA) = new LuaTable(instruction.B, instruction.C);
                        stack.NotifyTop(RA + 1);
                        break;
                    case OpCode.Self:
                        {
                            stack.EnsureCapacity(RA + 2);
                            var table = stack.UnsafeGet(RB);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            await GetTableValue(state, thread, chunk, rootChunk, pc, table, vc, resultBuffer.AsMemory(), cancellationToken);
                            var value = resultBuffer[0];

                            stack.UnsafeGet(RA + 1) = table;
                            stack.UnsafeGet(RA) = value;
                            stack.NotifyTop(RA + 2);
                        }
                        break;
                    case OpCode.Add:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                stack.UnsafeGet(RA) = valueB + valueC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Add, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Add, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "add", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Sub:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                stack.UnsafeGet(RA) = valueB - valueC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Sub, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Sub, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "sub", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Mul:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                stack.UnsafeGet(RA) = valueB * valueC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Mul, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Mul, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "mul", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Div:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                stack.UnsafeGet(RA) = valueB / valueC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Div, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Div, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "div", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Mod:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                var mod = valueB % valueC;
                                if ((valueC > 0 && mod < 0) || (valueC < 0 && mod > 0))
                                {
                                    mod += valueC;
                                }
                                stack.UnsafeGet(RA) = mod;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Mod, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Mod, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "mod", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Pow:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                stack.UnsafeGet(RA) = Math.Pow(valueB, valueC);
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Pow, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Pow, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "pow", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Unm:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = stack.UnsafeGet(RB);

                            if (vb.TryRead<double>(out var valueB))
                            {
                                stack.UnsafeGet(RA) = -valueB;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Unm, out var metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 1,
                                    FrameBase = stack.Count - 1,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "unm", vb);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Not:
                        {
                            stack.EnsureCapacity(RA + 1);
                            stack.UnsafeGet(RA) = !stack.UnsafeGet(RB).ToBoolean();
                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Len:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = stack.UnsafeGet(RB);

                            if (vb.TryRead<string>(out var str))
                            {
                                stack.UnsafeGet(RA) = str.Length;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Len, out var metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 1,
                                    FrameBase = stack.Count - 1,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else if (vb.TryRead<LuaTable>(out var table))
                            {
                                stack.UnsafeGet(RA) = table.ArrayLength;
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "get length of", vb);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Concat:
                        {
                            stack.EnsureCapacity(RA + 1);

                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);

                            var bIsValid = vb.TryRead<string>(out var strB);
                            var cIsValid = vc.TryRead<string>(out var strC);

                            if (!bIsValid && vb.TryRead<double>(out var numB))
                            {
                                strB = numB.ToString();
                                bIsValid = true;
                            }
                            if (!cIsValid && vc.TryRead<double>(out var numC))
                            {
                                strC = numC.ToString();
                                cIsValid = true;
                            }

                            if (bIsValid && cIsValid)
                            {
                                stack.UnsafeGet(RA) = strB + strC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Concat, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Concat, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                stack.UnsafeGet(RA) = resultBuffer[0];
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "concat", vb, vc);
                            }

                            stack.NotifyTop(RA + 1);
                        }
                        break;
                    case OpCode.Jmp:
                        pc += instruction.SBx;
                        if (instruction.A != 0)
                        {
                            state.CloseUpValues(thread, instruction.A - 1);
                        }
                        break;
                    case OpCode.Eq:
                        {
                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);
                            var compareResult = vb == vc;

                            if (!compareResult && (vb.TryGetMetamethod(state, Metamethods.Eq, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Eq, out metamethod)))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                compareResult = resultBuffer[0].ToBoolean();
                            }

                            if (compareResult != (instruction.A == 1))
                            {
                                pc++;
                            }
                        }
                        break;
                    case OpCode.Lt:
                        {
                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);
                            var compareResult = false;

                            if (vb.TryRead<string>(out var strB) && vc.TryRead<string>(out var strC))
                            {
                                compareResult = StringComparer.Ordinal.Compare(strB, strC) < 0;
                            }
                            else if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                compareResult = valueB < valueC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Lt, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Lt, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                compareResult = resultBuffer[0].ToBoolean();
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "less than", vb, vc);
                            }

                            if (compareResult != (instruction.A == 1))
                            {
                                pc++;
                            }
                        }
                        break;
                    case OpCode.Le:
                        {
                            var vb = RK(stack, chunk, instruction.B, frame.Base);
                            var vc = RK(stack, chunk, instruction.C, frame.Base);
                            var compareResult = false;

                            if (vb.TryRead<string>(out var strB) && vc.TryRead<string>(out var strC))
                            {
                                compareResult = StringComparer.Ordinal.Compare(strB, strC) <= 0;
                            }
                            else if (vb.TryRead<double>(out var valueB) && vc.TryRead<double>(out var valueC))
                            {
                                compareResult = valueB <= valueC;
                            }
                            else if (vb.TryGetMetamethod(state, Metamethods.Le, out var metamethod) || vc.TryGetMetamethod(state, Metamethods.Le, out metamethod))
                            {
                                if (!metamethod.TryRead<LuaFunction>(out var func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }

                                stack.Push(vb);
                                stack.Push(vc);

                                await func.InvokeAsync(new()
                                {
                                    State = state,
                                    Thread = thread,
                                    ArgumentCount = 2,
                                    FrameBase = stack.Count - 2,
                                    SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                    ChunkName = chunk.Name,
                                    RootChunkName = rootChunk.Name,
                                }, resultBuffer.AsMemory(), cancellationToken);

                                compareResult = resultBuffer[0].ToBoolean();
                            }
                            else
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "less than or equals", vb, vc);
                            }

                            if (compareResult != (instruction.A == 1))
                            {
                                pc++;
                            }
                        }
                        break;
                    case OpCode.Test:
                        {
                            if (stack.UnsafeGet(RA).ToBoolean() != (instruction.C == 1))
                            {
                                pc++;
                            }
                        }
                        break;
                    case OpCode.TestSet:
                        {
                            if (stack.UnsafeGet(RB).ToBoolean() != (instruction.C == 1))
                            {
                                pc++;
                            }
                            else
                            {
                                stack.UnsafeGet(RA) = stack.UnsafeGet(RB);
                                stack.NotifyTop(RA + 1);
                            }
                        }
                        break;
                    case OpCode.Call:
                        {
                            var va = stack.UnsafeGet(RA);
                            if (!va.TryRead<LuaFunction>(out var func))
                            {
                                if (va.TryGetMetamethod(state, Metamethods.Call, out var metamethod) && metamethod.TryRead<LuaFunction>(out func))
                                {
                                }
                                else
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }
                            }

                            (var newBase, var argumentCount) = PrepareForFunctionCall(thread, func, instruction, RA, resultBuffer.AsSpan(), false);

                            var rawResultCount = await func.InvokeAsync(new()
                            {
                                State = state,
                                Thread = thread,
                                ArgumentCount = argumentCount,
                                FrameBase = newBase,
                                SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                ChunkName = chunk.Name,
                                RootChunkName = rootChunk.Name,
                            }, resultBuffer.AsMemory(), cancellationToken);

                            var resultCount = rawResultCount;

                            if (instruction.C != 0)
                            {
                                resultCount = instruction.C - 1;
                            }

                            if (resultCount == 0)
                            {
                                stack.Pop();
                            }
                            else
                            {
                                stack.EnsureCapacity(RA + resultCount);
                                for (int i = 0; i < resultCount; i++)
                                {
                                    stack.UnsafeGet(RA + i) = i >= rawResultCount
                                        ? LuaValue.Nil
                                        : resultBuffer[i];
                                }
                                stack.NotifyTop(RA + resultCount);
                            }
                        }
                        break;
                    case OpCode.TailCall:
                        {
                            state.CloseUpValues(thread, frame.Base);

                            var va = stack.UnsafeGet(RA);
                            if (!va.TryRead<LuaFunction>(out var func))
                            {
                                if (!va.TryGetMetamethod(state, Metamethods.Call, out var metamethod) && !metamethod.TryRead<LuaFunction>(out func))
                                {
                                    LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
                                }
                            }

                            (var newBase, var argumentCount) = PrepareForFunctionCall(thread, func, instruction, RA, resultBuffer.AsSpan(), true);

                            return await func.InvokeAsync(new()
                            {
                                State = state,
                                Thread = thread,
                                ArgumentCount = argumentCount,
                                FrameBase = newBase,
                                SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                ChunkName = chunk.Name,
                                RootChunkName = rootChunk.Name,
                            }, buffer, cancellationToken);
                        }
                    case OpCode.Return:
                        {
                            state.CloseUpValues(thread, frame.Base);

                            var retCount = instruction.B - 1;

                            if (retCount == -1)
                            {
                                retCount = stack.Count - RA;
                            }

                            for (int i = 0; i < retCount; i++)
                            {
                                buffer.Span[i] = stack.UnsafeGet(RA + i);
                            }

                            return retCount;
                        }
                    case OpCode.ForLoop:
                        {
                            stack.EnsureCapacity(RA + 4);

                            if (!stack.UnsafeGet(RA).TryRead<double>(out var init))
                            {
                                throw new LuaRuntimeException(state.GetTraceback(), "'for' initial value must be a number");
                            }

                            if (!stack.UnsafeGet(RA + 1).TryRead<double>(out var limit))
                            {
                                throw new LuaRuntimeException(state.GetTraceback(), "'for' limit must be a number");
                            }

                            if (!stack.UnsafeGet(RA + 2).TryRead<double>(out var step))
                            {
                                throw new LuaRuntimeException(state.GetTraceback(), "'for' step must be a number");
                            }

                            var va = init + step;
                            stack.UnsafeGet(RA) = va;

                            if (step >= 0 ? va <= limit : va >= limit)
                            {
                                pc += instruction.SBx;
                                stack.UnsafeGet(RA + 3) = va;
                                stack.NotifyTop(RA + 4);
                            }
                            else
                            {
                                stack.NotifyTop(RA + 1);
                            }
                        }
                        break;
                    case OpCode.ForPrep:
                        {
                            if (!stack.UnsafeGet(RA).TryRead<double>(out var init))
                            {
                                throw new LuaRuntimeException(state.GetTraceback(), "'for' initial value must be a number");
                            }

                            if (!stack.UnsafeGet(RA + 2).TryRead<double>(out var step))
                            {
                                throw new LuaRuntimeException(state.GetTraceback(), "'for' step must be a number");
                            }

                            stack.UnsafeGet(RA) = init - step;
                            stack.NotifyTop(RA + 1);
                            pc += instruction.SBx;
                        }
                        break;
                    case OpCode.TForCall:
                        {
                            var iteratorRaw = stack.UnsafeGet(RA);
                            if (!iteratorRaw.TryRead<LuaFunction>(out var iterator))
                            {
                                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", iteratorRaw);
                            }

                            var nextBase = RA + 3 + instruction.C;
                            stack.UnsafeGet(nextBase) = stack.UnsafeGet(RA + 1);
                            stack.UnsafeGet(nextBase + 1) = stack.UnsafeGet(RA + 2);
                            stack.NotifyTop(nextBase + 2);

                            var resultCount = await iterator.InvokeAsync(new()
                            {
                                State = state,
                                Thread = thread,
                                ArgumentCount = 2,
                                FrameBase = nextBase,
                                SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                                ChunkName = chunk.Name,
                                RootChunkName = rootChunk.Name,
                            }, resultBuffer.AsMemory(), cancellationToken);

                            stack.EnsureCapacity(RA + instruction.C + 3);
                            for (int i = 1; i <= instruction.C; i++)
                            {
                                var index = i - 1;
                                stack.UnsafeGet(RA + 2 + i) = index >= resultCount
                                    ? LuaValue.Nil
                                    : resultBuffer[i - 1];
                            }
                            stack.NotifyTop(RA + instruction.C + 3);
                        }
                        break;
                    case OpCode.TForLoop:
                        {
                            var forState = stack.UnsafeGet(RA + 1);
                            if (forState.Type is not LuaValueType.Nil)
                            {
                                stack.UnsafeGet(RA) = forState;
                                pc += instruction.SBx;
                            }
                        }
                        break;
                    case OpCode.SetList:
                        {
                            if (!stack.UnsafeGet(RA).TryRead<LuaTable>(out var table))
                            {
                                throw new LuaException("internal error");
                            }

                            var count = instruction.B == 0
                                ? stack.Count - (RA + 1)
                                : instruction.B;

                            table.EnsureArrayCapacity((instruction.C - 1) * 50 + count);
                            stack.AsSpan().Slice(RA + 1, count)
                                .CopyTo(table.GetArraySpan()[((instruction.C - 1) * 50)..]);
                        }
                        break;
                    case OpCode.Closure:
                        stack.EnsureCapacity(RA + 1);
                        stack.UnsafeGet(RA) = new Closure(state, chunk.Functions[instruction.SBx]);
                        stack.NotifyTop(RA + 1);
                        break;
                    case OpCode.VarArg:
                        {
                            var count = instruction.B == 0
                                ? frame.VariableArgumentCount
                                : instruction.B - 1;

                            stack.EnsureCapacity(RA + count);
                            for (int i = 0; i < count; i++)
                            {
                                stack.UnsafeGet(RA + i) = frame.VariableArgumentCount > i
                                    ? stack.UnsafeGet(frame.Base - (frame.VariableArgumentCount - i))
                                    : LuaValue.Nil;
                            }
                            stack.NotifyTop(RA + count);
                        }
                        break;
                    case OpCode.ExtraArg:
                        throw new NotImplementedException();
                    default:
                        break;
                }
            }
        }
        catch (Exception)
        {
            state.CloseUpValues(thread, frame.Base);
            throw;
        }
        finally
        {
            ArrayPool<LuaValue>.Shared.Return(resultBuffer);
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ref LuaValue RK(LuaStack stack, Chunk chunk, ushort index, int frameBase)
    {
        if (index >= 256)
        {
            return ref MemoryMarshalEx.UnsafeElementAt(chunk.Constants, index - 256);
        }
        else
        {
            return ref stack.UnsafeGet(index + frameBase);
        }
    }

    static ValueTask<int> GetTableValue(LuaState state, LuaThread thread, Chunk chunk, Chunk rootChunk, int pc, LuaValue table, LuaValue key, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var stack = thread.Stack;
        var isTable = table.TryRead<LuaTable>(out var t);

        if (isTable && t.TryGetValue(key, out var result))
        {
            buffer.Span[0] = result;
            return new(1);
        }
        else if (table.TryGetMetamethod(state, Metamethods.Index, out var metamethod))
        {
            if (!metamethod.TryRead<LuaFunction>(out var indexTable))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
            }

            stack.Push(table);
            stack.Push(key);

            return indexTable.InvokeAsync(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = 2,
                SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                FrameBase = stack.Count - 2,
                ChunkName = chunk.Name,
                RootChunkName = rootChunk.Name,
            }, buffer, cancellationToken);
        }
        else if (isTable)
        {
            buffer.Span[0] = LuaValue.Nil;
            return new(1);
        }
        else
        {
            LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "index", table);
            return default; // dummy
        }
    }

    static ValueTask<int> SetTableValue(LuaState state, LuaThread thread, Chunk chunk, Chunk rootChunk, int pc, LuaValue table, LuaValue key, LuaValue value, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        var stack = thread.Stack;
        var isTable = table.TryRead<LuaTable>(out var t);

        if (key.Type is LuaValueType.Number)
        {
            var d = key.UnsafeRead<double>();
            if (double.IsNaN(d))
            {
                throw new LuaRuntimeException(GetTracebacks(state, chunk, pc), "table index is NaN");
            }
        }

        if (isTable)
        {
            t[key] = value;
            return new(1);
        }
        else if (table.TryGetMetamethod(state, Metamethods.NewIndex, out var metamethod))
        {
            if (!metamethod.TryRead<LuaFunction>(out var indexTable))
            {
                LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "call", metamethod);
            }

            stack.Push(table);
            stack.Push(key);
            stack.Push(value);

            return indexTable.InvokeAsync(new()
            {
                State = state,
                Thread = thread,
                ArgumentCount = 3,
                FrameBase = stack.Count - 3,
                SourcePosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
                ChunkName = chunk.Name,
                RootChunkName = rootChunk.Name,
            }, buffer, cancellationToken);
        }
        else
        {
            LuaRuntimeException.AttemptInvalidOperation(GetTracebacks(state, chunk, pc), "index", table);
            return default; // dummy
        }
    }

    static (int FrameBase, int ArgumentCount) PrepareForFunctionCall(LuaThread thread, LuaFunction function, Instruction instruction, int RA, Span<LuaValue> buffer, bool isTailCall)
    {
        var stack = thread.Stack;

        var argumentCount = instruction.B - 1;
        if (instruction.B == 0)
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

        // If there are variable arguments, the base of the stack is moved by that number and the values ​​of the variable arguments are placed in front of it.
        // see: https://wubingzheng.github.io/build-lua-in-rust/en/ch08-02.arguments.html
        if (variableArgumentCount > 0)
        {
            var temp = newBase;
            newBase += variableArgumentCount;

            stack.EnsureCapacity(newBase + argumentCount);
            stack.NotifyTop(newBase + argumentCount);

            var stackBuffer = stack.GetBuffer();
            stackBuffer.Slice(temp, argumentCount).CopyTo(buffer);
            buffer.Slice(0, argumentCount).CopyTo(stackBuffer[newBase..]);

            buffer.Slice(argumentCount - variableArgumentCount, variableArgumentCount).CopyTo(stackBuffer[temp..]);
        }

        return (newBase, argumentCount);
    }

    static Traceback GetTracebacks(LuaState state, Chunk chunk, int pc)
    {
        var frame = state.CurrentThread.GetCurrentFrame();
        state.CurrentThread.PushCallStackFrame(frame with
        {
            CallPosition = MemoryMarshalEx.UnsafeElementAt(chunk.SourcePositions, pc),
            ChunkName = chunk.Name,
            RootChunkName = chunk.GetRoot().Name,
        });
        var tracebacks = state.GetTraceback();
        state.CurrentThread.PopCallStackFrame();

        return tracebacks;
    }
}