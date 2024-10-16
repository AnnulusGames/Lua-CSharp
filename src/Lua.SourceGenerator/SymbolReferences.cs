using Microsoft.CodeAnalysis;

namespace Lua.SourceGenerator;

public sealed class SymbolReferences
{
    public static SymbolReferences? Create(Compilation compilation)
    {
        var luaObjectAttribute = compilation.GetTypeByMetadataName("Lua.LuaObjectAttribute");
        if (luaObjectAttribute == null) return null;

        return new SymbolReferences
        {
            LuaObjectAttribute = luaObjectAttribute,
            LuaMemberAttribute = compilation.GetTypeByMetadataName("Lua.LuaMemberAttribute")!,
            LuaIgnoreMemberAttribute = compilation.GetTypeByMetadataName("Lua.LuaIgnoreMemberAttribute")!,
        };
    }

    public INamedTypeSymbol LuaObjectAttribute { get; private set; } = default!;
    public INamedTypeSymbol LuaMemberAttribute { get; private set; } = default!;
    public INamedTypeSymbol LuaIgnoreMemberAttribute { get; private set; } = default!;
}