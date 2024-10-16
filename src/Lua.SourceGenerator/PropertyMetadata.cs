using Microsoft.CodeAnalysis;

namespace Lua.SourceGenerator;

public class PropertyMetadata
{
    public ISymbol Symbol { get; }
    public string TypeFullName { get; }
    public bool IsStatic { get; }
    public bool IsReadOnly { get; }
    public string LuaMemberName { get; }

    public PropertyMetadata(ISymbol symbol, SymbolReferences references)
    {
        Symbol = symbol;

        IsStatic = symbol.IsStatic;

        if (symbol is IFieldSymbol field)
        {
            TypeFullName = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            IsReadOnly = field.IsReadOnly;
        }
        else if (symbol is IPropertySymbol property)
        {
            TypeFullName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            IsReadOnly = property.SetMethod == null;
        }
        else
        {
            TypeFullName = "";
        }

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