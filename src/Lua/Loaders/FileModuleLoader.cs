
namespace Lua.Loaders;

public sealed class FileModuleLoader : ILuaModuleLoader
{
    public static readonly FileModuleLoader Instance = new();

    public bool Exists(string moduleName)
    {
        return File.Exists(moduleName);
    }

    public async ValueTask<LuaValue> LoadAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(moduleName, cancellationToken);
    }
}