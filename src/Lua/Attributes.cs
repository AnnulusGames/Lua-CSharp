namespace Lua;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LuaObjectAttribute : Attribute
{
    public LuaObjectAttribute()
    {
    }

    public LuaObjectAttribute(string name)
    {
        Name = name;
    }

    public string? Name { get; }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LuaMemberAttribute : Attribute
{
    public LuaMemberAttribute()
    {
    }

    public LuaMemberAttribute(string name)
    {
        Name = name;
    }

    public string? Name { get; }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LuaIgnoreMemberAttribute : Attribute
{
}