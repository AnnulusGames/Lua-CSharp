namespace Lua.Unity
{
    public static class LuaStateExtensions
    {
        public static void OpenUnityLibrary(this LuaState state)
        {
            var vector3 = new LuaTable(0, Vector3Library.Instance.Functions.Length);
            foreach (var func in Vector3Library.Instance.Functions)
            {
                vector3[func.Name] = func;
            }
            state.Environment["vector3"] = vector3;
        }
    }
}