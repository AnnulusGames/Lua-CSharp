using System;
using System.Threading;
using System.Threading.Tasks;
using Lua.Runtime;
using UnityEngine;

namespace Lua.Unity
{
    public sealed class Vector3Library
    {
        public static readonly Vector3Library Instance = new();

        public readonly LuaFunction[] Functions;
        public readonly LuaTable Metatable = new();

        public Vector3Library()
        {
            Functions = new LuaFunction[]
            {
                new("angle", Angle),
                new("cross", Cross),
                new("distance", Distance),
                new("dot", Dot),
                new("lerp", Lerp),
                new("lerp_unclamped", LerpUnclamped),
                new("max", Max),
                new("min", Min),
                new("move_towards", MoveTowards),
                new("project", Project),
                new("project_on_plane", ProjectOnPlane),
                new("reflect", Reflect),
                new("rotate_towards", RotateTowards),
                new("scale", Scale),
                new("signed_angle", SignedAngle),
                new("slerp", Slerp),
                new("slerp_unclamped", SlerpUnclamped),
            };

            Metatable[Metamethods.Index] = new LuaFunction((context, buffer, ct) =>
            {
                var name = context.GetArgument<string>(1);
                buffer.Span[0] = name switch
                {
                    "zero" => new LuaVector3(Vector3.zero),
                    "one" => new LuaVector3(Vector3.one),
                    "right" => new LuaVector3(Vector2.right),
                    "left" => new LuaVector3(Vector3.left),
                    "up" => new LuaVector3(Vector3.up),
                    "down" => new LuaVector3(Vector3.down),
                    "forward" => new LuaVector3(Vector3.forward),
                    "back" => new LuaVector3(Vector3.back),
                    _ => LuaValue.Nil,
                };
                return new(1);
            });
        }

        public ValueTask<int> Angle(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = Vector3.Angle(a, b);
            return new(1);
        }

        public ValueTask<int> Cross(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.Cross(a, b));
            return new(1);
        }

        public ValueTask<int> Distance(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = Vector3.Distance(a, b);
            return new(1);
        }

        public ValueTask<int> Dot(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = Vector3.Dot(a, b);
            return new(1);
        }

        public ValueTask<int> Lerp(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector3(Vector3.Lerp(a, b, t));
            return new(1);
        }

        public ValueTask<int> LerpUnclamped(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector3(Vector3.LerpUnclamped(a, b, t));
            return new(1);
        }

        public ValueTask<int> Max(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.Max(a, b));
            return new(1);
        }

        public ValueTask<int> Min(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.Min(a, b));
            return new(1);
        }

        public ValueTask<int> MoveTowards(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector3(Vector3.MoveTowards(a, b, t));
            return new(1);
        }

        public ValueTask<int> Project(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var vector = context.GetArgument<LuaVector3>(0);
            var onNormal = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.Project(vector, onNormal));
            return new(1);
        }

        public ValueTask<int> ProjectOnPlane(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var vector = context.GetArgument<LuaVector3>(0);
            var planeNormal = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.ProjectOnPlane(vector, planeNormal));
            return new(1);
        }

        public ValueTask<int> Reflect(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var inDirection = context.GetArgument<LuaVector3>(0);
            var inNormal = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.Reflect(inDirection, inNormal));
            return new(1);
        }

        public ValueTask<int> RotateTowards(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var current = context.GetArgument<LuaVector3>(0);
            var target = context.GetArgument<LuaVector3>(1);
            var maxRadiansDelta = context.GetArgument<float>(2);
            var maxMagnitudeDelta = context.GetArgument<float>(3);
            buffer.Span[0] = new LuaVector3(Vector3.RotateTowards(current, target, maxRadiansDelta, maxMagnitudeDelta));
            return new(1);
        }

        public ValueTask<int> Scale(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            buffer.Span[0] = new LuaVector3(Vector3.Scale(a, b));
            return new(1);
        }

        public ValueTask<int> SignedAngle(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var from = context.GetArgument<LuaVector3>(0);
            var to = context.GetArgument<LuaVector3>(1);
            var axis = context.GetArgument<LuaVector3>(2);
            buffer.Span[0] = Vector3.SignedAngle(from, to, axis);
            return new(1);
        }

        public ValueTask<int> Slerp(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector3(Vector3.Slerp(a, b, t));
            return new(1);
        }

        public ValueTask<int> SlerpUnclamped(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector3>(0);
            var b = context.GetArgument<LuaVector3>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector3(Vector3.SlerpUnclamped(a, b, t));
            return new(1);
        }
    }

    [LuaObject]
    public sealed partial class LuaVector3
    {
        Vector3 value;

        [LuaMember("x")]
        public float X
        {
            get => value.x;
            set => this.value.x = value;
        }

        [LuaMember("y")]
        public float Y
        {
            get => value.y;
            set => this.value.y = value;
        }

        [LuaMember("z")]
        public float Z
        {
            get => value.z;
            set => this.value.z = value;
        }

        public LuaVector3(float x, float y, float z)
        {
            value = new Vector3(x, y, z);
        }

        public LuaVector3(Vector3 vector3)
        {
            value = vector3;
        }

        [LuaMember("normalized")]
        public LuaVector3 Normalized() => new(value.normalized);

        [LuaMember("magnitude")]
        public float Magnitude() => value.magnitude;

        [LuaMember("sqrmagnitude")]
        public float SqrMagnitude() => value.sqrMagnitude;

        [LuaMetamethod(LuaObjectMetamethod.Add)]
        public static LuaVector3 Add(LuaVector3 a, LuaVector3 b)
        {
            return new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        [LuaMetamethod(LuaObjectMetamethod.Sub)]
        public static LuaVector3 Sub(LuaVector3 a, LuaVector3 b)
        {
            return new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        [LuaMetamethod(LuaObjectMetamethod.Mul)]
        public static LuaVector3 Mul(LuaVector3 a, float b)
        {
            return new(a.X * b, a.Y * b, a.Z * b);
        }

        [LuaMetamethod(LuaObjectMetamethod.Div)]
        public static LuaVector3 Div(LuaVector3 a, float b)
        {
            return new(a.X / b, a.Y / b, a.Z / b);
        }

        [LuaMetamethod(LuaObjectMetamethod.ToString)]
        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator Vector3(LuaVector3 luaVector3)
        {
            return new Vector3(luaVector3.X, luaVector3.Y, luaVector3.Z);
        }

        public static implicit operator LuaVector3(Vector3 luaVector3)
        {
            return new LuaVector3(luaVector3.x, luaVector3.y, luaVector3.z);
        }
    }
}