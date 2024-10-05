using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lua.Internal;
using Lua.Runtime;

namespace Lua;

public enum LuaValueType : byte
{
    Nil,
    Boolean,
    String,
    Number,
    Function,
    Thread,
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
                if (t == typeof(float))
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
                else if (t == typeof(double))
                {
                    var str = (string)referenceValue!;
                    var span = str.AsSpan().Trim();

                    var sign = 1;
                    if (span.Length > 0 && span[0] == '-')
                    {
                        sign = -1;
                        span = span[1..];
                    }

                    if (span.Length > 2 && span[0] is '0' && span[1] is 'x' or 'X')
                    {
                        // TODO: optimize
                        try
                        {
                            var d = HexConverter.ToDouble(span) * sign;
                            result = Unsafe.As<double, T>(ref d);
                            return true;
                        }
                        catch (FormatException)
                        {
                            result = default!;
                            return false;
                        }
                    }
                    else
                    {
                        var tryResult = double.TryParse(str, out var d);
                        result = tryResult ? Unsafe.As<double, T>(ref d) : default!;
                        return tryResult;
                    }
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
            case LuaValueType.Thread:
                if (t == typeof(LuaThread))
                {
                    var v = (LuaThread)referenceValue!;
                    result = Unsafe.As<LuaThread, T>(ref v);
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
                if (t == typeof(LuaUserData) || t.IsSubclassOf(typeof(LuaUserData)))
                {
                    var v = (LuaUserData)referenceValue!;
                    result = Unsafe.As<LuaUserData, T>(ref v);
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

    public LuaValue(LuaThread value)
    {
        type = LuaValueType.Thread;
        referenceValue = value;
    }

    public LuaValue(LuaUserData value)
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

    public static implicit operator LuaValue(LuaThread value)
    {
        return new(value);
    }

    public static implicit operator LuaValue(LuaUserData value)
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
            LuaValueType.Function or LuaValueType.Thread or LuaValueType.Table or LuaValueType.UserData => referenceValue!.GetHashCode(),
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
            LuaValueType.Thread => Read<LuaThread>().Equals(other.Read<LuaThread>()),
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
            LuaValueType.Nil => "nil",
            LuaValueType.Boolean => Read<bool>() ? "true" : "false",
            LuaValueType.String => Read<string>(),
            LuaValueType.Number => Read<double>().ToString(),
            LuaValueType.Function => $"function: {referenceValue!.GetHashCode()}",
            LuaValueType.Thread => $"thread: {referenceValue!.GetHashCode()}",
            LuaValueType.Table => $"table: {referenceValue!.GetHashCode()}",
            LuaValueType.UserData => $"userdata: {referenceValue!.GetHashCode()}",
            _ => "",
        };
    }

    public static bool TryGetLuaValueType(Type type, out LuaValueType result)
    {
        if (type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long))
        {
            result = LuaValueType.Number;
            return true;
        }
        else if (type == typeof(bool))
        {
            result = LuaValueType.Boolean;
            return true;
        }
        else if (type == typeof(string))
        {
            result = LuaValueType.String;
            return true;
        }
        else if (type == typeof(LuaFunction) || type.IsSubclassOf(typeof(LuaFunction)))
        {
            result = LuaValueType.Function;
            return true;
        }
        else if (type == typeof(LuaTable))
        {
            result = LuaValueType.Table;
            return true;
        }
        else if (type == typeof(LuaThread))
        {
            result = LuaValueType.Thread;
            return true;
        }

        result = default;
        return false;
    }

    internal ValueTask<int> CallToStringAsync(LuaFunctionExecutionContext context, Memory<LuaValue> buffer, CancellationToken cancellationToken)
    {
        if (this.TryGetMetamethod(context.State, Metamethods.ToString, out var metamethod))
        {
            if (!metamethod.TryRead<LuaFunction>(out var func))
            {
                LuaRuntimeException.AttemptInvalidOperation(context.State.GetTraceback(), "call", metamethod);
            }

            context.State.Push(this);

            return func.InvokeAsync(context with
            {
                ArgumentCount = 1,
                StackPosition = context.State.CurrentThread.Stack.Count,
            }, buffer, cancellationToken);
        }
        else
        {
            buffer.Span[0] = ToString()!;
            return new(1);
        }
    }
}