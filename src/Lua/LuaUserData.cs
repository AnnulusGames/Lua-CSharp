namespace Lua;

public abstract class LuaUserData
{
    public LuaTable? Metatable { get; set; }
}

public class LuaUserData<T>(T value) : LuaUserData
{
    public T Value { get; } = value;
}