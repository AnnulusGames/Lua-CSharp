using Microsoft.CodeAnalysis;

namespace Lua.SourceGenerator;

internal static class RoslynAnalyzerExtensions
{
    public static AttributeData? FindAttribute(this IEnumerable<AttributeData> attributeDataList, string typeName)
    {
        return attributeDataList
            .Where(x => x.AttributeClass?.ToDisplayString() == typeName)
            .FirstOrDefault();
    }

    public static AttributeData? FindAttributeShortName(this IEnumerable<AttributeData> attributeDataList, string typeName)
    {
        return attributeDataList
            .Where(x => x.AttributeClass?.Name == typeName)
            .FirstOrDefault();
    }
}