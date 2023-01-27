using System;
using System.Collections.Generic;
using System.Linq;

internal static class Utils
{
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
        if (list.IsEmpty())
        {
            throw new ArgumentNullException(nameof(enumerable));
        }
        return list[r.Next(0, list.Count)];
    }
}