namespace Lua.Unity
{
    public static class LuaStateExtensions
    {
        public static void OpenUnityLibraries(this LuaState state)
        {
            var vector2 = new LuaTable(0, Vector2Library.Instance.Functions.Length);
            foreach (var func in Vector2Library.Instance.Functions)
            {
                vector2[func.Name] = func;
            }
            state.Environment["vector2"] = vector2;

            var vector3 = new LuaTable(0, Vector3Library.Instance.Functions.Length);
            foreach (var func in Vector3Library.Instance.Functions)
            {
                vector3[func.Name] = func;
            }
            state.Environment["vector3"] = vector3;

            var color = new LuaTable(0, ColorLibrary.Instance.Functions.Length);
            foreach (var func in ColorLibrary.Instance.Functions)
            {
                color[func.Name] = func;
            }
            state.Environment["color"] = color;
        }
    }
}