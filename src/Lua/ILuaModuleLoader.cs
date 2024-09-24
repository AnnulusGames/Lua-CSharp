namespace Lua;

public interface ILuaModuleLoader
{
    bool Exists(string moduleName);
    ValueTask<LuaValue> LoadAsync(string moduleName, CancellationToken cancellationToken = default);
}