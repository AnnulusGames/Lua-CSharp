using Lua.Runtime;
using UnityEngine;

namespace Lua.Unity
{
    public sealed class TimeLibrary
    {
        public static readonly TimeLibrary Instance = new();

        public readonly LuaTable Metatable = new();

        public TimeLibrary()
        {
            Metatable[Metamethods.Index] = new LuaFunction((context, buffer, ct) =>
            {
                var name = context.GetArgument<string>(1);
                buffer.Span[0] = name switch
                {
                    "time" => Time.timeAsDouble,
                    "unscaled_time" => Time.unscaledTimeAsDouble,
                    "delta_time" => Time.deltaTime,
                    "unscaled_delta_time" => Time.unscaledDeltaTime,
                    "fixed_time" => Time.fixedTimeAsDouble,
                    "fixed_unscaled_time" => Time.fixedUnscaledTimeAsDouble,
                    "fixed_delta_time" => Time.fixedDeltaTime,
                    "fixed_unscaled_delta_time" => Time.fixedUnscaledDeltaTime,
                    "time_since_level_load" => Time.timeSinceLevelLoadAsDouble,
                    "in_fixed_time_step" => Time.inFixedTimeStep,
                    "frame_count" => Time.frameCount,
                    "time_scale" => Time.timeScale,
                    _ => LuaValue.Nil,
                };
                return new(1);
            });

            Metatable[Metamethods.NewIndex] = new LuaFunction((context, buffer, ct) =>
            {
                var name = context.GetArgument<string>(1);
                switch (name)
                {
                    case "time_scale":
                        Time.timeScale = context.GetArgument<float>(2);
                        break;
                }
                return new(0);
            });
        }
    }
}