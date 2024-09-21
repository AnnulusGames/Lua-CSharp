namespace Lua.Tests;

public class LuaTests
{
    [Test]
    public async Task Test_Closure()
    {
        await LuaState.Create().DoFileAsync(FileHelper.GetAbsolutePath("tests-lua/closure.lua"));
    }
}