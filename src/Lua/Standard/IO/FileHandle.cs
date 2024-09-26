namespace Lua.Standard.IO;

public class FileHandle(FileStream stream) : LuaUserData
{
    public FileStream Stream { get; } = stream;
}