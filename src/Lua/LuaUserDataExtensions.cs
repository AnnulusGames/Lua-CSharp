namespace Lua;

public static class LuaUserDataExtensions
{
    public static LuaValue AsLuaValue(this ILuaUserData userData)
    {
        return new(userData);
    }
}