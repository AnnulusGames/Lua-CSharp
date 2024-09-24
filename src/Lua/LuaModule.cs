namespace Lua;

public enum LuaModuleType
{
    Text,
}

public readonly struct LuaModule
{
    public string Name => name;
    public LuaModuleType Type => type;

    readonly string name;
    readonly LuaModuleType type;
    readonly object referenceValue;

    public LuaModule(string name, string text)
    {
        this.name = name;
        type = LuaModuleType.Text;
        referenceValue = text;
    }

    public string ReadText()
    {
        if (type != LuaModuleType.Text) throw new Exception(); // TODO: add message
        return (string)referenceValue;
    }
}