using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lua.Unity
{
    public sealed class Vector3Library
    {
        public static readonly Vector3Library Instance = new();

        public readonly LuaFunction[] Functions;

        public Vector3Library()
        {
            Functions = new LuaFunction[]
            {
                new("zero", Zero),
                new("one", One),
                new("right", Right),
                new("left", Left),
                new("up", Up),
                new("down", Down),
                new("forward", Forward),
                new("back", Back),
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
        }

        public ValueTask<int> Zero(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.zero);
            return new(1);
        }

        public ValueTask<int> One(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.one);
            return new(1);
        }

        public ValueTask<int> Right(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.right);
            return new(1);
        }

        public ValueTask<int> Left(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.left);
            return new(1);
        }

        public ValueTask<int> Up(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.up);
            return new(1);
        }

        public ValueTask<int> Down(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.down);
            return new(1);
        }

        public ValueTask<int> Forward(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.forward);
            return new(1);
        }

        public ValueTask<int> Back(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector3(Vector3.back);
            return new(1);
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
        Vector3 vector;

        [LuaMember("x")]
        public float X
        {
            get => vector.x;
            set => vector.x = value;
        }

        [LuaMember("y")]
        public float Y
        {
            get => vector.y;
            set => vector.y = value;
        }

        [LuaMember("z")]
        public float Z
        {
            get => vector.z;
            set => vector.z = value;
        }

        public LuaVector3(float x, float y, float z)
        {
            vector = new Vector3(x, y, z);
        }

        public LuaVector3(Vector3 vector3)
        {
            vector = vector3;
        }

        [LuaMember("normalized")]
        public LuaVector3 Normalized() => new(vector.normalized);

        [LuaMember("magnitude")]
        public float Magnitude() => vector.magnitude;

        [LuaMember("sqrmagnitude")]
        public float SqrMagnitude() => vector.sqrMagnitude;

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
            return vector.ToString();
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