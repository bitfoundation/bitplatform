using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the <c>bit-css-hide-*</c> utility classes (<see cref="BitCss.Class.Hide"/>) against the compiled core bundle: each
/// one hides its element in a media query alone - from its breakpoint up, or below it - on the default breakpoints of
/// <see cref="BitThemeBreakpointDefaults"/>, and with the weight to win over a component's own display.
/// </summary>
[TestClass]
public sealed class BitCssHideClassesTests
{
    private const string Hidden = "{display:none!important}";

    public static IEnumerable<object[]> FromBreakpointUp =>
    [
        [BitCss.Class.Hide.Sm, BitThemeBreakpointDefaults.Sm],
        [BitCss.Class.Hide.Md, BitThemeBreakpointDefaults.Md],
        [BitCss.Class.Hide.Lg, BitThemeBreakpointDefaults.Lg],
        [BitCss.Class.Hide.Xl, BitThemeBreakpointDefaults.Xl],
        [BitCss.Class.Hide.Xxl, BitThemeBreakpointDefaults.Xxl],
    ];

    public static IEnumerable<object[]> BelowBreakpoint =>
    [
        [BitCss.Class.Hide.LtSm, BitThemeBreakpointDefaults.Sm],
        [BitCss.Class.Hide.LtMd, BitThemeBreakpointDefaults.Md],
        [BitCss.Class.Hide.LtLg, BitThemeBreakpointDefaults.Lg],
        [BitCss.Class.Hide.LtXl, BitThemeBreakpointDefaults.Xl],
        [BitCss.Class.Hide.LtXxl, BitThemeBreakpointDefaults.Xxl],
    ];

    [TestMethod]
    public void HideXsHidesAtEveryWidth()
    {
        Assert.AreEqual("bit-css-hide-xs", BitCss.Class.Hide.Xs);

        StringAssert.Contains(CompiledCoreCss(), $"}}.{BitCss.Class.Hide.Xs}{Hidden}",
            "bit-css-hide-xs is the base of the from-a-breakpoint-up classes and hides outside any media query.");
    }

    [TestMethod]
    [DynamicData(nameof(FromBreakpointUp))]
    public void HidesFromItsBreakpointUp(string className, string breakpoint)
    {
        StringAssert.Contains(CompiledCoreCss(), $"@media(min-width:{breakpoint}){{.{className}{Hidden}}}");
    }

    [TestMethod]
    [DynamicData(nameof(BelowBreakpoint))]
    public void HidesBelowItsBreakpoint(string className, string breakpoint)
    {
        // The upper bound sits 0.02px under the breakpoint, as media-queries.scss and BitMediaQuery write it.
        var max = (int.Parse(breakpoint.Replace("px", string.Empty), System.Globalization.CultureInfo.InvariantCulture) - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".98px";

        StringAssert.Contains(CompiledCoreCss(), $"@media(max-width:{max}){{.{className}{Hidden}}}");
    }

    [TestMethod]
    public void EveryHideClassIsNamedAfterItsConstant()
    {
        var names = typeof(BitCss.Class.Hide).GetFields().Select(f => (f.Name, Value: (string)f.GetRawConstantValue()!)).ToArray();

        Assert.AreEqual(11, names.Length);

        foreach (var (name, value) in names)
        {
            var expected = name.StartsWith("Lt", StringComparison.Ordinal)
                ? $"bit-css-hide-lt-{name[2..].ToLowerInvariant()}"
                : $"bit-css-hide-{name.ToLowerInvariant()}";

            Assert.AreEqual(expected, value, $"BitCss.Class.Hide.{name}");
        }
    }

    private static string CompiledCoreCss()
        => Regex.Replace(SourceFiles.Read("Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css"), @"\s+", string.Empty);
}
