using Microsoft.CodeAnalysis;

namespace Lua.SourceGenerator;

internal class MethodMetadata
{
    public IMethodSymbol Symbol { get; }
    public bool IsStatic { get; }
    public bool HasMemberAttribute { get; }
    public bool HasMetamethodAttribute { get; }
    public string LuaMemberName { get; }
    public LuaObjectMetamethod Metamethod { get; }

    public MethodMetadata(IMethodSymbol symbol, SymbolReferences references)
    {
        Symbol = symbol;
        IsStatic = symbol.IsStatic;

        LuaMemberName = symbol.Name;

        var memberAttribute = symbol.GetAttribute(references.LuaMemberAttribute);
        HasMemberAttribute = memberAttribute != null;

        if (memberAttribute != null)
        {
            if (memberAttribute.ConstructorArguments.Length > 0)
            {
                var value = memberAttribute.ConstructorArguments[0].Value;
                if (value is string str)
                {
                    LuaMemberName = str;
                }
            }
        }

        var metamethodAttribute = symbol.GetAttribute(references.LuaMetamethodAttribute);
        HasMetamethodAttribute = metamethodAttribute != null;

        if (metamethodAttribute != null)
        {
            Metamethod = (LuaObjectMetamethod)Enum.Parse(typeof(LuaObjectMetamethod), metamethodAttribute.ConstructorArguments[0].Value!.ToString());
        }
    }
}