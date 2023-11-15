using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Breezy.Muticaster
{
    internal static class EnumerableExtensions
    {
        public static bool IsEmpty<TSource>([NotNullWhen(false)]this IEnumerable<TSource> source)
        {
            return source is null || !source.Any();
        }
    }
}
