using System.Runtime.CompilerServices;
using Lua.Internal;

namespace Lua;

public sealed class LuaTable
{
    public LuaTable() : this(8, 8)
    {
    }

    public LuaTable(IEnumerable<LuaValue> values)
    {
        array = values.ToArray();
        dictionary = [];
    }

    public LuaTable(IEnumerable<KeyValuePair<LuaValue, LuaValue>> values)
    {
        array = [];
        dictionary = new Dictionary<LuaValue, LuaValue>(values);
    }

    public LuaTable(int arrayCapacity, int dictionaryCapacity)
    {
        array = new LuaValue[arrayCapacity];
        dictionary = new(dictionaryCapacity);
    }

    LuaValue[] array;
    Dictionary<LuaValue, LuaValue> dictionary;
    LuaTable? metatable;

    public LuaValue this[LuaValue key]
    {
        get
        {
            if (key.Type is LuaValueType.Nil)
            {
                throw new ArgumentException("table index is nil");
            }

            if (TryGetInteger(key, out var index))
            {
                if (index > 0 && index <= array.Length)
                {
                    // Arrays in Lua are 1-origin...
                    return array[index - 1];
                }
            }
            
            if (dictionary.TryGetValue(key, out var value)) return value;
            return LuaValue.Nil;
        }
        set
        {
            if (TryGetInteger(key, out var index))
            {
                if (0 < index && index <= array.Length * 2)
                {
                    EnsureArrayCapacity(index);
                    array[index - 1] = value;
                    return;
                }
            }

            if (value.Type is LuaValueType.Nil)
            {
                dictionary.Remove(key);
            }
            else
            {
                dictionary[key] = value;
            }
        }
    }

    public int HashMapCount
    {
        get => dictionary.Count;
    }

    public int ArrayLength
    {
        get
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Type is LuaValueType.Nil) return i;
            }
            return array.Length;
        }
    }

    public LuaTable? Metatable
    {
        get => metatable;
        set => metatable = value;
    }

    public bool TryGetValue(LuaValue key, out LuaValue value)
    {
        if (key.Type is LuaValueType.Nil)
        {
            value = default;
            return false;
        }

        if (TryGetInteger(key, out var index))
        {
            if (index > 0 && index <= array.Length)
            {
                value = array[index - 1];
                return value.Type is not LuaValueType.Nil;
            }
        }

        return dictionary.TryGetValue(key, out value);
    }

    public bool ContainsKey(LuaValue key)
    {
        if (key.Type is LuaValueType.Nil)
        {
            return false;
        }

        if (TryGetInteger(key, out var index))
        {
            return index > 0 && index <= array.Length && array[index].Type != LuaValueType.Nil;
        }

        return dictionary.ContainsKey(key);
    }

    public KeyValuePair<LuaValue, LuaValue> GetNext(LuaValue key)
    {
        var index = -1;
        if (key.Type is LuaValueType.Nil)
        {
            index = 0;
        }
        else if (TryGetInteger(key, out var integer) && integer > 0 && integer <= array.Length)
        {
            index = integer;
        }

        if (index != -1)
        {
            var span = array.AsSpan(index);
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Type is not LuaValueType.Nil)
                {
                    return new(index + i + 1, span[i]);
                }
            }

            foreach (var pair in dictionary)
            {
                return pair;
            }
        }
        else
        {
            var foundKey = false;
            foreach (var pair in dictionary)
            {
                if (foundKey) return pair;

                if (pair.Key.Equals(key))
                {
                    foundKey = true;
                }
            }
        }

        return default;
    }

    public void Clear()
    {
        dictionary.Clear();
    }

    public Span<LuaValue> GetArraySpan()
    {
        return array.AsSpan();
    }
    
    internal void EnsureArrayCapacity(int newCapacity)
    {
        if (array.Length >= newCapacity) return;

        var prevLength = array.Length;
        var newLength = array.Length;
        if (newLength == 0) newLength = 8;

        while (newLength < newCapacity)
        {
            newLength *= 2;
        }

        Array.Resize(ref array, newLength);

        using var indexList = new PooledList<(int, LuaValue)>(dictionary.Count);

        // Move some of the elements of the hash part to a newly allocated array
        foreach (var kv in dictionary)
        {
            if (kv.Key.TryRead<double>(out var d) && MathEx.IsInteger(d))
            {
                var index = (int)d;
                if (index > prevLength && index <= newLength)
                {
                    indexList.Add((index, kv.Value));
                }
            }
        }

        foreach ((var index, var value) in indexList.AsSpan())
        {
            dictionary.Remove(index);
            array[index - 1] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryGetInteger(LuaValue value, out int integer)
    {
        if (value.TryRead<double>(out var num) && MathEx.IsInteger(num))
        {
            integer = (int)num;
            return true;
        }

        integer = default;
        return false;
    }
}