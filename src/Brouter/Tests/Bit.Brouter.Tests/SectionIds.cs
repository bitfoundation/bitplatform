namespace Bit.Brouter.Tests;

/// <summary>
/// Section identifiers shared by the section-outlet regression tests (issue #12752): the layout's
/// SectionOutlet and every page's SectionContent reference the same identity object, mirroring the
/// common shared-footer pattern.
/// </summary>
public static class SectionIds
{
    public static readonly object Footer = new();
}
