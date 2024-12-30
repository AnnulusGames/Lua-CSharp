using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lua.Unity
{
    public sealed class Vector2Library
    {
        public static readonly Vector2Library Instance = new();

        public readonly LuaFunction[] Functions;

        public Vector2Library()
        {
            Functions = new LuaFunction[]
            {
                new("zero", Zero),
                new("one", One),
                new("right", Right),
                new("left", Left),
                new("up", Up),
                new("down", Down),
                new("angle", Angle),
                new("distance", Distance),
                new("dot", Dot),
                new("lerp", Lerp),
                new("lerp_unclamped", LerpUnclamped),
                new("max", Max),
                new("min", Min),
                new("move_towards", MoveTowards),
                new("reflect", Reflect),
                new("scale", Scale),
                new("signed_angle", SignedAngle),
            };
        }

        public ValueTask<int> Zero(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector2(Vector2.zero);
            return new(1);
        }

        public ValueTask<int> One(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector2(Vector2.one);
            return new(1);
        }

        public ValueTask<int> Right(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector2(Vector2.right);
            return new(1);
        }

        public ValueTask<int> Left(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector2(Vector2.left);
            return new(1);
        }

        public ValueTask<int> Up(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector2(Vector2.up);
            return new(1);
        }

        public ValueTask<int> Down(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaVector2(Vector2.down);
            return new(1);
        }

        public ValueTask<int> Angle(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = Vector2.Angle(a, b);
            return new(1);
        }
        public ValueTask<int> Distance(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = Vector2.Distance(a, b);
            return new(1);
        }

        public ValueTask<int> Dot(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = Vector2.Dot(a, b);
            return new(1);
        }

        public ValueTask<int> Lerp(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector2(Vector2.Lerp(a, b, t));
            return new(1);
        }

        public ValueTask<int> LerpUnclamped(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector2(Vector2.LerpUnclamped(a, b, t));
            return new(1);
        }

        public ValueTask<int> Max(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = new LuaVector2(Vector2.Max(a, b));
            return new(1);
        }

        public ValueTask<int> Min(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = new LuaVector2(Vector2.Min(a, b));
            return new(1);
        }

        public ValueTask<int> MoveTowards(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            var t = context.GetArgument<float>(2);
            buffer.Span[0] = new LuaVector2(Vector2.MoveTowards(a, b, t));
            return new(1);
        }

        public ValueTask<int> Reflect(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var inDirection = context.GetArgument<LuaVector2>(0);
            var inNormal = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = new LuaVector2(Vector2.Reflect(inDirection, inNormal));
            return new(1);
        }

        public ValueTask<int> Scale(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var a = context.GetArgument<LuaVector2>(0);
            var b = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = new LuaVector2(Vector2.Scale(a, b));
            return new(1);
        }

        public ValueTask<int> SignedAngle(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var from = context.GetArgument<LuaVector2>(0);
            var to = context.GetArgument<LuaVector2>(1);
            buffer.Span[0] = Vector2.SignedAngle(from, to);
            return new(1);
        }
    }

    [LuaObject]
    public sealed partial class LuaVector2
    {
        Vector2 value;

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

        public LuaVector2(float x, float y)
        {
            value = new Vector2(x, y);
        }

        public LuaVector2(Vector2 vector2)
        {
            value = vector2;
        }

        [LuaMember("normalized")]
        public LuaVector2 Normalized() => new(value.normalized);

        [LuaMember("magnitude")]
        public float Magnitude() => value.magnitude;

        [LuaMember("sqrmagnitude")]
        public float SqrMagnitude() => value.sqrMagnitude;

        [LuaMetamethod(LuaObjectMetamethod.Add)]
        public static LuaVector2 Add(LuaVector2 a, LuaVector2 b)
        {
            return new(a.X + b.X, a.Y + b.Y);
        }

        [LuaMetamethod(LuaObjectMetamethod.Sub)]
        public static LuaVector2 Sub(LuaVector2 a, LuaVector2 b)
        {
            return new(a.X - b.X, a.Y - b.Y);
        }

        [LuaMetamethod(LuaObjectMetamethod.Mul)]
        public static LuaVector2 Mul(LuaVector2 a, float b)
        {
            return new(a.X * b, a.Y * b);
        }

        [LuaMetamethod(LuaObjectMetamethod.Div)]
        public static LuaVector2 Div(LuaVector2 a, float b)
        {
            return new(a.X / b, a.Y / b);
        }

        [LuaMetamethod(LuaObjectMetamethod.ToString)]
        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator Vector2(LuaVector2 luaVector2)
        {
            return new Vector2(luaVector2.X, luaVector2.Y);
        }

        public static implicit operator LuaVector2(Vector2 luaVector2)
        {
            return new LuaVector2(luaVector2.x, luaVector2.y);
        }
    }
}