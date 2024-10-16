using Lua;

[LuaObject("vec3")]
public partial class LVec3
{
    [LuaMember("x")]
    public double X { get; set; }

    [LuaMember("y")]
    public double Y { get; set; }

    [LuaMember("z")]
    public double Z { get; set; }

    [LuaMember("create")]
    public static LVec3 Create(double x, double y, double z)
    {
        return new LVec3()
        {
            X = x,
            Y = y,
            Z = z,
        };
    }
}