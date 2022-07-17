using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Bit.BlazorUI.Playground.Web")]
namespace Bit.BlazorUI;

internal static class StringExtensions
{
    internal static bool HasValue(this string? value, bool ignoreWhiteSpace = true)
    {
        return ignoreWhiteSpace
            ? string.IsNullOrWhiteSpace(value) is false
            : string.IsNullOrEmpty(value) is false;
    }

    internal static bool HasNoValue(this string? value, bool ignoreWhiteSpace = true)
    {
        return ignoreWhiteSpace
            ? string.IsNullOrWhiteSpace(value)
            : string.IsNullOrEmpty(value);
    }
}
