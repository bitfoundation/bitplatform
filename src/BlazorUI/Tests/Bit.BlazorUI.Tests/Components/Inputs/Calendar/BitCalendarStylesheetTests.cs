using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Calendar;

/// <summary>
/// Pins the order the calendar's stylesheet reads its colors and sizes in, which a bUnit render cannot see: what an
/// explicit Color or Size publishes is read before the public --bit-Calendar-* variable restyling it, and the default
/// an unset one stands for comes last.
/// </summary>
[TestClass]
public class BitCalendarStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Calendar", "BitCalendar.scss");

    [TestMethod]
    public void BitCalendarShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // size an unset one stands for.
        Assert.Contains("--bit-cal-cell-size: var(--bit-cal-cell-size-sz, var(--bit-Calendar-day-size, #{spacing(3.5)}));", stylesheet);
        Assert.Contains("--bit-cal-cell-fs: var(--bit-cal-cell-fs-sz, var(--bit-Calendar-day-font-size, #{$tg-fs-sm}));", stylesheet);

        // So does an explicit Color, for every color it paints.
        Assert.Contains("var(--bit-cal-clr, var(--bit-Calendar-today-background, #{$clr-pri}))", stylesheet);
        Assert.Contains("var(--bit-cal-clr-txt, var(--bit-Calendar-today-color, #{$clr-pri-text}))", stylesheet);
        Assert.Contains("var(--bit-cal-clr-hover, var(--bit-Calendar-today-hover-background, #{$clr-pri-hover}))", stylesheet);
        Assert.Contains("var(--bit-cal-clr-active, var(--bit-Calendar-today-active-background, #{$clr-pri-active}))", stylesheet);
        Assert.Contains("focus-ring-own(var(--bit-cal-clr-focus, var(--bit-Calendar-focus-color)))", stylesheet);

        // An event's own color comes before both.
        Assert.Contains("var(--bit-cal-evt-clr, var(--bit-cal-clr, var(--bit-Calendar-event-color, #{$clr-pri})))", stylesheet);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Calendar-[a-z-]+, var\(--bit-cal-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitCalendarShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cal {");

        // A calendar can sit in a template of another one, which must not inherit the outer calendar's Color or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at the
        // same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-cal-clr", "--bit-cal-clr-txt", "--bit-cal-clr-hover", "--bit-cal-clr-active", "--bit-cal-clr-focus",
                                         "--bit-cal-evt-clr", "--bit-cal-cell-size-sz", "--bit-cal-cell-fs-sz" })
        {
            Assert.Contains($"{property}: initial;", root);
        }

        Assert.IsLessThan(stylesheet.IndexOf("\n    .bit-cal-#{$role} {", System.StringComparison.Ordinal), stylesheet.IndexOf("\n.bit-cal {", System.StringComparison.Ordinal), "The root that resets what the role classes publish comes before them, so they still win on the root that carries them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-cal-sm {", System.StringComparison.Ordinal), stylesheet.IndexOf("\n.bit-cal {", System.StringComparison.Ordinal), "The root that resets what the size classes publish comes before them, so they still win on the root that carries them.");
    }
}
