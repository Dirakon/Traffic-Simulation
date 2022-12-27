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
}