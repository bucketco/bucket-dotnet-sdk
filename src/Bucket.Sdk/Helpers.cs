namespace Bucket.Sdk;

internal static class Helpers
{
    public static bool ToStringElementWise<T>(
        this IEnumerable<T> enumerable, StringBuilder builder)
    {
        Debug.Assert(enumerable != null);
        Debug.Assert(builder != null);

        var str = string.Join(", ", enumerable);
        _ = builder.Append(str);

        return str.Length > 0;
    }

    public static bool ToStringElementWise<TValue>(
        this IEnumerable<KeyValuePair<string, TValue>> enumerable,
        StringBuilder builder)
    {
        Debug.Assert(enumerable != null);
        Debug.Assert(builder != null);

        var processed = enumerable
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"\"{kv.Key}\" = {kv.Value}");

        var str = string.Join(", ", processed);
        _ = builder.Append(str);

        return str.Length > 0;
    }

    public static string ToStringElementWise<T>(this IEnumerable<T> enumerable)
    {
        var builder = new StringBuilder("[ ");
        var length = builder.Length;

        _ = enumerable.ToStringElementWise(builder);
        if (builder.Length > length)
        {
            _ = builder.Append(' ');
        }

        _ = builder.Append(']');

        return builder.ToString();
    }

    public static string ToStringElementWise<TValue>(this IEnumerable<KeyValuePair<string, TValue>> enumerable)
    {
        var builder = new StringBuilder("{ ");
        var length = builder.Length;

        _ = enumerable.ToStringElementWise(builder);
        if (builder.Length > length)
        {
            _ = builder.Append(' ');
        }

        _ = builder.Append('}');

        return builder.ToString();
    }

    public static int GetHashCodeElementWise<TValue>(this IEnumerable<KeyValuePair<string, TValue>> enumerable)
    {
        Debug.Assert(enumerable != null);

        var hash = new HashCode();
        foreach (var (key, value) in enumerable.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            hash.Add(key);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public static int GetHashCodeElementWise<T>(
        this IEnumerable<T> enumerable)
    {
        Debug.Assert(enumerable != null);

        var hash = new HashCode();
        foreach (var value in enumerable.Order())
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public static bool EqualsElementWise<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        IReadOnlyDictionary<TKey, TValue> other)
    {
        Debug.Assert(dictionary != null);
        Debug.Assert(other != null);

        if (dictionary.Count != other.Count)
        {
            return false;
        }

        foreach (var kv in dictionary)
        {
            if (!other.TryGetValue(kv.Key, out var value) || !Equals(kv.Value, value))
            {
                return false;
            }
        }

        return true;
    }

    public static bool EqualsElementWise<T>(
        this IReadOnlyList<T> list, IReadOnlyList<T> other)
    {
        Debug.Assert(list != null);
        Debug.Assert(other != null);


        return list.Count == other.Count &&
               !list.Where((t, i) => !Equals(t, other[i])).Any();
    }
}
