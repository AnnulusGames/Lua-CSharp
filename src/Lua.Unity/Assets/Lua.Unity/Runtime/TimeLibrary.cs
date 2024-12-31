using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lua.Unity
{
    public sealed class TimeLibrary
    {
        public static readonly TimeLibrary Instance = new();

        public readonly LuaFunction[] functions = new LuaFunction[]
        {
            new("time", GetTime),
            new("delta_time", GetDeltaTime),
        };

        public static ValueTask<int> GetTime(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = Time.timeAsDouble;
            return new(1);
        }

        public static ValueTask<int> GetDeltaTime(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = Time.deltaTime;
            return new(1);
        }

    }
}