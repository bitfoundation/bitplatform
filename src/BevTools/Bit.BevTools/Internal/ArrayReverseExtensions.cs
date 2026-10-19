#if !NET10_0_OR_GREATER
namespace Bit.BevTools;

/// <summary>
/// C# 14's first-class span conversions bind <c>array.Reverse()</c> to the in-place, void
/// <c>MemoryExtensions.Reverse(Span&lt;T&gt;)</c>; .NET 10 added <c>Enumerable.Reverse(T[])</c> to win that overload
/// resolution back for LINQ. The store snapshots are arrays read newest-first all over the panels, so the older target
/// frameworks get the same overload here, closer in scope than <c>System</c>.
/// </summary>
internal static class ArrayReverseExtensions
{
    public static IEnumerable<T> Reverse<T>(this T[] array) => Enumerable.Reverse(array);
}
#endif
