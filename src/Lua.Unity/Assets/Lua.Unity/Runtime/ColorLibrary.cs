using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lua.Unity
{
    public sealed class ColorLibrary
    {
        public static readonly ColorLibrary Instance = new();

        public readonly LuaFunction[] Functions;

        public ColorLibrary()
        {
            Functions = new LuaFunction[]
            {
                new("black", Black),
                new("blue", Blue),
                new("clear", Clear),
                new("cyan", Cyan),
                new("gray", Gray),
                new("green", Green),
                new("magenta", Magenta),
                new("red", Red),
                new("white", White),
                new("yellow", Yellow),
                new("hsv_to_rgb", HSVToRGB),
                new("rgb_to_hsv", RGBToHSV),
            };
        }

        public static ValueTask<int> Black(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.black);
            return new(1);
        }

        public static ValueTask<int> Blue(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.blue);
            return new(1);
        }

        public static ValueTask<int> Clear(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.clear);
            return new(1);
        }

        public static ValueTask<int> Cyan(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.cyan);
            return new(1);
        }

        public static ValueTask<int> Gray(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.gray);
            return new(1);
        }

        public static ValueTask<int> Green(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.green);
            return new(1);
        }

        public static ValueTask<int> Magenta(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.magenta);
            return new(1);
        }

        public static ValueTask<int> Red(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.red);
            return new(1);
        }

        public static ValueTask<int> White(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.white);
            return new(1);
        }

        public static ValueTask<int> Yellow(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            buffer.Span[0] = new LuaColor(Color.yellow);
            return new(1);
        }

        public static ValueTask<int> HSVToRGB(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var h = context.GetArgument<float>(0);
            var s = context.GetArgument<float>(1);
            var v = context.GetArgument<float>(2);
            var hdr = context.HasArgument(3)
                ? context.GetArgument<bool>(3)
                : false;

            buffer.Span[0] = new LuaColor(Color.HSVToRGB(h, s, v, hdr));
            return new(1);
        }

        public static ValueTask<int> RGBToHSV(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
        {
            var color = context.GetArgument<LuaColor>(0);
            Color.RGBToHSV(color, out var h, out var s, out var v);
            buffer.Span[0] = h;
            buffer.Span[1] = s;
            buffer.Span[2] = v;
            return new(3);
        }
    }

    [LuaObject]
    public sealed partial class LuaColor
    {
        Color value;

        public LuaColor(Color color)
        {
            value = color;
        }

        public LuaColor(float r, float g, float b, float a)
        {
            value = new(r, g, b, a);
        }

        [LuaMember("r")]
        public float R
        {
            get => value.r;
            set => this.value.r = value;
        }

        [LuaMember("g")]
        public float G
        {
            get => value.g;
            set => this.value.g = value;
        }

        [LuaMember("b")]
        public float B
        {
            get => value.b;
            set => this.value.b = value;
        }

        [LuaMember("a")]
        public float A
        {
            get => value.a;
            set => this.value.a = value;
        }

        [LuaMetamethod(LuaObjectMetamethod.ToString)]
        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator Color(LuaColor color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        public static implicit operator LuaColor(Color color)
        {
            return new LuaColor(color);
        }
    }
}