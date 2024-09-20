namespace Lua.Tests;

public class TableTests
{
    [Test]
    public void Test_Indexer()
    {
        var table = new LuaTable();
        table[1] = "foo";
        table["bar"] = 2;
        table[true] = "baz";

        Assert.That(table[1], Is.EqualTo(new LuaValue("foo")));
        Assert.That(table["bar"], Is.EqualTo(new LuaValue(2)));
        Assert.That(table[true], Is.EqualTo(new LuaValue("baz")));
    }
}