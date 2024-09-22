namespace Lua;

public enum LuaThreadStatus : byte
{
    Normal,
    Suspended,
    Running,
    Dead,
}