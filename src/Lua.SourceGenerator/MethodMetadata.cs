using Microsoft.CodeAnalysis;

namespace Lua.SourceGenerator;

public class MethodMetadata
{
    public IMethodSymbol Symbol { get; }
    public bool IsStatic { get; }
    public string LuaMemberName { get; }

    public MethodMetadata(IMethodSymbol symbol, SymbolReferences references)
    {
        Symbol = symbol;
        IsStatic = symbol.IsStatic;

        LuaMemberName = symbol.Name;

        var memberAttribute = symbol.GetAttribute(references.LuaMemberAttribute);
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
    }
}