using System;
using System.Collections.Generic;
using System.Linq;

internal static class LinqUtils
{
    public static TSource? MinByOrDefault<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector) where TSource : struct
    {
        var list = source as IList<TSource> ?? source.ToList();
        return list.IsEmpty() ? default(TSource?) : list.MinBy(keySelector);
    }

    // An example of a bad language design for C#. The following function gives an error,
    // even though it clearly works for reference types only, while the function above works for value types only...
    //
    // public static TSource? MinByOrDefault<TSource,TKey> (
    //     this IEnumerable<TSource> source,
    //     Func<TSource, TKey> keySelector) where TSource : class
    // {
    //     var list = source as IList<TSource> ?? source.ToList();
    //     return list.IsEmpty() ? default(TSource?) : list.MinBy(keySelector);
    // }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : struct
    {
        return enumerable.Where(e => e != null).Select(e => e!.Value);
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable) where T : class
    {
        return enumerable.Where(e => e != null).Select(e => e!);
    }

    public static bool IsEmpty<T>(this IEnumerable<T?> enumerable)
    {
        return !enumerable.Any();
    }

    public static T Random<T>(this IEnumerable<T> enumerable)
    {
        // note: creating a Random instance each call may not be correct for you,
        // consider a thread-safe static instance
        var r = new Random();
        var list = enumerable as IList<T> ?? enumerable.ToList();
        if (list.IsEmpty()) throw new ArgumentNullException(nameof(enumerable));
        return list[r.Next(0, list.Count)];
    }

    public static (IEnumerable<TSource> passed, IEnumerable<TSource> failed) Split<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        var lookup = source.ToLookup(predicate);
        return (passed: lookup[true], failed: lookup[false]);
    }


    public static TSource? MaxByOrDefault<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        var list = source as IList<TSource> ?? source.ToList();
        return list.IsEmpty() ? default : list.MaxBy(keySelector);
    }

    public static T? ToNullable<T>(this T source) where T : struct
    {
        return source;
    }
}