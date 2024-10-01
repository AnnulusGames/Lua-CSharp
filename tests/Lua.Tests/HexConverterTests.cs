using Lua.Internal;

namespace Lua.Tests;

public class HexConverterTests
{
    [TestCase("0x10", 16)]
    [TestCase("0x0p12", 0)]
    [TestCase("-0x1.0p-1", -0.5)]
    public void Test_ToDouble(string text, double expected)
    {
        Assert.That(HexConverter.ToDouble(text), Is.EqualTo(expected));
    }
}