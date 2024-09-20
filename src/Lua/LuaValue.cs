using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lua;

public enum LuaValueType : byte
{
    Nil,
    Boolean,
    String,
    Number,
    Function,
    UserData,
    Table,
}

[StructLayout(LayoutKind.Auto)]
public readonly struct LuaValue : IEquatable<LuaValue>
{
    public static readonly LuaValue Nil = default;

    readonly LuaValueType type;
    readonly double value;
    readonly object? referenceValue;

    public LuaValueType Type => type;

    public bool TryRead<T>(out T result)
    {
        var t = typeof(T);

        switch (type)
        {
            case LuaValueType.Number:
                if (t == typeof(int))
                {
                    var v = (int)value;
                    result = Unsafe.As<int, T>(ref v);
                    return true;
                }
                else if (t == typeof(long))
                {
                    var v = (long)value;
                    result = Unsafe.As<long, T>(ref v);
                    return true;
                }
                else if (t == typeof(float))
                {
                    var v = (float)value;
                    result = Unsafe.As<float, T>(ref v);
                    return true;
                }
                else if (t == typeof(double))
                {
                    var v = value;
                    result = Unsafe.As<double, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)(object)value;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.Boolean:
                if (t == typeof(bool))
                {
                    var v = value == 1;
                    result = Unsafe.As<bool, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)(object)value;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.String:
                if (t == typeof(string))
                {
                    var v = (string)referenceValue!;
                    result = Unsafe.As<string, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)referenceValue!;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.Function:
                if (t == typeof(LuaFunction) || t.IsSubclassOf(typeof(LuaFunction)))
                {
                    var v = (LuaFunction)referenceValue!;
                    result = Unsafe.As<LuaFunction, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)referenceValue!;
                    return true;
                }
                else
                {
                    break;
                }
            case LuaValueType.UserData:
                if (referenceValue is T userData)
                {
                    result = userData;
                    return true;
                }
                break;
            case LuaValueType.Table:
                if (t == typeof(LuaTable))
                {
                    var v = (LuaTable)referenceValue!;
                    result = Unsafe.As<LuaTable, T>(ref v);
                    return true;
                }
                else if (t == typeof(object))
                {
                    result = (T)referenceValue!;
                    return true;
                }
                else
                {
                    break;
                }
        }

        result = default!;
        return false;
    }

    public T Read<T>()
    {
        if (!TryRead<T>(out var result)) throw new InvalidOperationException($"Cannot convert LuaValueType.{Type} to {typeof(T).FullName}.");
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ToBoolean()
    {
        if (Type is LuaValueType.Nil) return false;
        if (TryRead<bool>(out var result)) return result;
        return true;
    }

    public LuaValue(bool value)
    {
        type = LuaValueType.Boolean;
        this.value = value ? 1 : 0;
    }

    public LuaValue(double value)
    {
        type = LuaValueType.Number;
        this.value = value;
    }

    public LuaValue(string value)
    {
        type = LuaValueType.String;
        referenceValue = value;
    }

    public LuaValue(LuaFunction value)
    {
        type = LuaValueType.Function;
        referenceValue = value;
    }

    public LuaValue(LuaTable value)
    {
        type = LuaValueType.Table;
        referenceValue = value;
    }

    public LuaValue(object? value)
    {
        type = LuaValueType.UserData;
        referenceValue = value;
    }

    public static implicit operator LuaValue(bool value)
    {
        return new(value);
    }

    public static implicit operator LuaValue(double value)
    {
        return new(value);
    }

    public static implicit operator LuaValue(string value)
    {
        return new(value);
    }

    public static implicit operator LuaValue(LuaTable value)
    {
        return new(value);
    }

    public static implicit operator LuaValue(LuaFunction value)
    {
        return new(value);
    }

    public override int GetHashCode()
    {
        var valueHash = type switch
        {
            LuaValueType.Nil => 0,
            LuaValueType.Boolean => Read<bool>().GetHashCode(),
            LuaValueType.String => Read<string>().GetHashCode(),
            LuaValueType.Number => Read<double>().GetHashCode(),
            LuaValueType.Function => Read<LuaFunction>().GetHashCode(),
            LuaValueType.Table => Read<LuaTable>().GetHashCode(),
            LuaValueType.UserData => referenceValue == null ? 0 : referenceValue.GetHashCode(),
            _ => 0,
        };

        return HashCode.Combine(type, valueHash);
    }

    public bool Equals(LuaValue other)
    {
        if (other.Type != Type) return false;

        return type switch
        {
            LuaValueType.Nil => true,
            LuaValueType.Boolean => Read<bool>().Equals(other.Read<bool>()),
            LuaValueType.String => Read<string>().Equals(other.Read<string>()),
            LuaValueType.Number => Read<double>().Equals(other.Read<double>()),
            LuaValueType.Function => Read<LuaFunction>().Equals(other.Read<LuaFunction>()),
            LuaValueType.Table => Read<LuaTable>().Equals(other.Read<LuaTable>()),
            LuaValueType.UserData => referenceValue == other.referenceValue,
            _ => false,
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is LuaValue value1 && Equals(value1);
    }

    public static bool operator ==(LuaValue a, LuaValue b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(LuaValue a, LuaValue b)
    {
        return !a.Equals(b);
    }

    public override string? ToString()
    {
        return type switch
        {
            LuaValueType.Nil => "Nil",
            LuaValueType.Boolean => Read<bool>().ToString(),
            LuaValueType.String => Read<string>().ToString(),
            LuaValueType.Number => Read<double>().ToString(),
            LuaValueType.Function => Read<LuaFunction>().ToString(),
            LuaValueType.Table => Read<LuaTable>().ToString(),
            LuaValueType.UserData => referenceValue?.ToString(),
            _ => "",
        };
    }
}