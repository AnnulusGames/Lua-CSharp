namespace Lua.Standard;

public static class OpenLibsExtensions
{
    public static void OpenStandardLibraries(this LuaState state)
    {
        state.OpenBasicLibrary();
        state.OpenBitwiseLibrary();
        state.OpenCoroutineLibrary();
        state.OpenIOLibrary();
        state.OpenMathLibrary();
        state.OpenModuleLibrary();
        state.OpenOperatingSystemLibrary();
        state.OpenStringLibrary();
        state.OpenTableLibrary();
    }
}