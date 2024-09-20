using Lua.Standard.Base;

namespace Lua.Standard;

public static class OpenLibExtensions
{
    public static void OpenBaseLibrary(this LuaState state)
    {
        state.Environment["_G"] = state.Environment;
        state.Environment["_VERSION"] = "Lua 5.2";
        state.Environment[AssertFunction.Name] = AssertFunction.Instance;
        state.Environment[ErrorFunction.Name] = ErrorFunction.Instance;
        state.Environment[PrintFunction.Name] = PrintFunction.Instance;
        state.Environment[RawGetFunction.Name] = RawGetFunction.Instance;
        state.Environment[RawSetFunction.Name] = RawSetFunction.Instance;
        state.Environment[GetMetatableFunction.Name] = GetMetatableFunction.Instance;
        state.Environment[SetMetatableFunction.Name] = SetMetatableFunction.Instance;
        state.Environment[ToStringFunction.Name] = ToStringFunction.Instance;
    }
}