using Microsoft.Azure.Cosmos;
using System.Diagnostics.CodeAnalysis;

namespace Breezy.Muticaster
{
    internal static class StringExtensions
    {
        public static bool IsEmpty<TSource>([NotNullWhen(false)]this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }

        public static PartitionKey? ToPartitionKey(this string source)
        {
            return source.IsEmpty() ? null : new PartitionKey(source);
        }
    }
}