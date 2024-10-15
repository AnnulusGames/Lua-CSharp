namespace Lua;

public interface ILuaUserData
{
    LuaTable? Metatable { get; set; }
}