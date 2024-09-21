using Lua.Standard.Base;

namespace Lua.Standard;

public static class OpenLibExtensions
{
    static readonly LuaFunction[] baseFunctions = [
        AssertFunction.Instance,
        ErrorFunction.Instance,
        PrintFunction.Instance,
        RawGetFunction.Instance,
        RawSetFunction.Instance,
        GetMetatableFunction.Instance,
        SetMetatableFunction.Instance,
        ToStringFunction.Instance
    ];

    public static void OpenBaseLibrary(this LuaState state)
    {
        state.Environment["_G"] = state.Environment;
        state.Environment["_VERSION"] = "Lua 5.2";
        foreach (var func in baseFunctions)
        {
            state.Environment[func.Name] = func;
        }
    }
}