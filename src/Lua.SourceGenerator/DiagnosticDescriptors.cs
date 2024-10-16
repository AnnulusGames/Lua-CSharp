using Microsoft.CodeAnalysis;

namespace Lua.SourceGenerator;

public static class DiagnosticDescriptors
{
    const string Category = "Lua";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "LUACS001",
        title: "LuaObject type must be partial.",
        category: Category,
        messageFormat: "LuaObject type '{0}' must be partial",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedNotAllowed = new(
        id: "LUACS002",
        title: "LuaObject type must not be nested",
        messageFormat: "LuaObject type '{0}' must be not nested",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AbstractNotAllowed = new(
        id: "LUAC003",
        title: "LuaObject type must not abstract",
        messageFormat: "LuaObject object '{0}' must be not abstract",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidPropertyType = new(
        id: "LUAC004",
        title: "The type of the field or property must be LuaValue or a type that can be converted to LuaValue.",
        messageFormat: "The type of the field or property must be LuaValue or a type that can be converted to LuaValue.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMethodType = new(
        id: "LUAC005",
        title: "The arguments and return types must be LuaValue or types that can be converted to LuaValue.",
        messageFormat: "The arguments and return types must be LuaValue or types that can be converted to LuaValue.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}