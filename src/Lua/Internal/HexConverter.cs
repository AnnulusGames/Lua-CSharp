using System.Globalization;
using System.Numerics;

namespace Lua.Internal;

public static class HexConverter
{
    public static double ToDouble(ReadOnlySpan<char> text)
    {
        var sign = 1;
        if (text[0] == '-')
        {
            // Remove the "-0x"
            sign = -1;
            text = text[3..];
        }
        else
        {
            // Remove the "0x"
            text = text[2..];
        }

        var dotIndex = text.IndexOf('.');
        var expIndex = text.IndexOfAny('p', 'P');

        if (dotIndex == -1 && expIndex == -1)
        {
            return (double)BigInteger.Parse(text, NumberStyles.HexNumber);
        }

        var intPart = dotIndex == -1 ? [] : text[..dotIndex];
        var decimalPart = expIndex == -1
            ? text.Slice(dotIndex + 1)
            : text.Slice(dotIndex + 1, expIndex - dotIndex - 1);
        var expPart = expIndex == -1 ? [] : text[(expIndex + 1)..];

        var value = intPart.Length == 0
            ? 0
            : long.Parse(intPart, NumberStyles.HexNumber);

        var decimalValue = 0.0;
        for (int i = 0; i < decimalPart.Length; i++)
        {
            decimalValue += ToInt(decimalPart[i]) * Math.Pow(16, -(i + 1));
        }

        double result = value + decimalValue;

        if (expPart.Length > 0)
        {
            result *= Math.Pow(2, int.Parse(expPart));
        }

        return result * sign;
    }

    static int ToInt(char c)
    {
        return c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            '9' => 9,
            'A' or 'a' => 10,
            'B' or 'b' => 11,
            'C' or 'd' => 12,
            'D' or 'e' => 13,
            'E' or 'e' => 14,
            'F' or 'f' => 15,
            _ => 0
        };
    }
}