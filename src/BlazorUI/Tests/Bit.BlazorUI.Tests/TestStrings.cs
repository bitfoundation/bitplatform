namespace Bit.BlazorUI.Tests;

internal static class TestStrings
{
    // U+202F NARROW NO-BREAK SPACE and U+00A0 NO-BREAK SPACE, written as code points so that the two
    // characters this deals with are visible in the source rather than hiding as spaces in a literal.
    private const char NarrowNoBreakSpace = (char)0x202F;
    private const char NoBreakSpace = (char)0x00A0;

    /// <summary>
    /// Brings the space characters a culture writes a time with onto the plain space the expectations
    /// of the tests are written with.
    /// </summary>
    /// <remarks>
    /// ICU 72 and later separate the AM/PM designator from the rest of a time with a narrow no-break
    /// space in en-US and the cultures that follow it, where older ICU versions and NLS use a plain
    /// space - so the same culture spells the same time differently on a Linux CI agent and on a
    /// Windows machine. The components write times the way their culture writes them, which is what
    /// the tests are checking, so the comparisons are made insensitive to which of the two spaces the
    /// machine running them happens to produce.
    /// </remarks>
    public static string NormalizeSpaces(this string? value)
    {
        return value is null ? string.Empty : value.Replace(NarrowNoBreakSpace, ' ').Replace(NoBreakSpace, ' ');
    }
}
