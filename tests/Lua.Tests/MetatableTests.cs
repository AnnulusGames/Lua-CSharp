using Lua.Standard;

namespace Lua.Tests;

public class MetatableTests
{
    LuaState state = default!;

    [OneTimeSetUp]
    public void SetUp()
    {
        state = LuaState.Create();
        state.OpenBaseLibrary();
    }

    [Test]
    public async Task Test_Metamethod_Add()
    {
        var source = @"
metatable = {
    __add = function(a, b)
        local t = { }
        for i = 1, #a do
            t[i] = a[i] + b[i]
        end
        return t
    end
}

local a = { 1, 2, 3 }
local b = { 4, 5, 6 }

setmetatable(a, metatable)

return a + b
";

        var result = await state.DoStringAsync(source);
        Assert.That(result, Has.Length.EqualTo(1));

        var table = result[0].Read<LuaTable>();
        Assert.Multiple(() =>
        {
            Assert.That(table[1].Read<double>(), Is.EqualTo(5));
            Assert.That(table[2].Read<double>(), Is.EqualTo(7));
            Assert.That(table[3].Read<double>(), Is.EqualTo(9));
        });
    }
}