using System.ComponentModel;

namespace Bit.BlazorUI;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool HasValue(this string? value, bool ignoreWhiteSpace = true)
    {
        return ignoreWhiteSpace
            ? string.IsNullOrWhiteSpace(value) is false
            : string.IsNullOrEmpty(value) is false;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool HasNoValue(this string? value, bool ignoreWhiteSpace = true)
    {
        return ignoreWhiteSpace
            ? string.IsNullOrWhiteSpace(value)
            : string.IsNullOrEmpty(value);
    }
}
