using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that replacing the global <c>--bit-shd-focus-ring</c> re-shapes the focus ring of every component that has not
/// been given a focus color of its own.
/// </summary>
/// <remarks>
/// The composite substitutes its var()s where it is declared, on :root, so a color set further down the page cannot get
/// into it: a component whose ring has a color of its own has to compose that ring itself. <c>focus-ring($color)</c>
/// always does, and so always beats the global token; <c>focus-ring-own($color)</c> does only while something in the
/// color chain is set, and hands the ring back to the composite otherwise. A component focus color is therefore drawn
/// with the latter, and its chain carries no fallback to the primary focus color - that fallback is what kept the ring
/// from ever being the global one.
/// </remarks>
[TestClass]
public sealed class BitFocusRingStylesheetTests
{
    private static readonly Regex ColoredFocusRing = new(@"@include focus-ring\((?<args>[^;]+)\);", RegexOptions.Compiled);

    private static readonly Regex OwnFocusRing = new(@"@include focus-ring-own\((?<args>[^;]+)\);", RegexOptions.Compiled);

    // A ring a component keeps on purpose, whatever the global token says. The error and the disabled colors mean
    // something; a zero offset places the ring against its element (a grid cell), a shape the composite cannot take.
    private static readonly Regex StateColor = new(@"err|invalid|error|dis", RegexOptions.Compiled);

    private static readonly Regex ZeroOffset = new(@",\s*0$", RegexOptions.Compiled);

    // A default color that is not the primary focus color, so not one the global ring could stand in for: the variant's
    // own color of a ButtonGroup item, and the Info role an unset Color stands for on a Message.
    private static readonly string[] OwnDefaultColors = ["--bit-btg-itm-clr-fcs", "--bit-msg-focus"];

    [TestMethod]
    public void EveryComponentFocusColorShouldGiveTheGlobalRingBackWhileItIsUnset()
    {
        var offenders = new List<string>();

        foreach (var (file, stylesheet) in ReadComponentStylesheets())
        {
            foreach (Match call in ColoredFocusRing.Matches(stylesheet))
            {
                var args = call.Groups["args"].Value;

                if (StateColor.IsMatch(args) || ZeroOffset.IsMatch(args) || OwnDefaultColors.Any(args.Contains)) continue;

                offenders.Add($"{Path.GetFileName(file)}: {call.Value}");
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A component draws its focus color with focus-ring(), which never reads --bit-shd-focus-ring - use focus-ring-own(): {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void NoFocusRingOwnColorShouldFallBackToThePrimaryFocusColor()
    {
        var offenders = new List<string>();

        foreach (var (file, stylesheet) in ReadComponentStylesheets())
        {
            foreach (Match call in OwnFocusRing.Matches(stylesheet))
            {
                // The chain itself, and the alias it reads - a root custom property or a Sass variable - wherever the
                // stylesheet declares one: a fallback on any of them resolves the ring for good.
                var color = call.Groups["args"].Value;
                var declarations = Regex.Matches(color, @"var\((?<name>--bit-[a-z][a-z0-9-]*)\)")
                                        .Select(m => m.Groups["name"].Value)
                                        .SelectMany(name => Regex.Matches(stylesheet, $@"{Regex.Escape(name)}\s*:\s*(?<value>[^;]+);").Select(m => m.Groups["value"].Value))
                                        .Concat(Regex.Matches(color, @"(?<name>\$[a-z][a-z0-9-]*)")
                                                     .SelectMany(m => Regex.Matches(stylesheet, $@"^{Regex.Escape(m.Groups["name"].Value)}\s*:\s*(?<value>[^;]+);", RegexOptions.Multiline))
                                                     .Select(m => m.Groups["value"].Value));

                if (declarations.Prepend(color).Any(value => value.Contains("$clr-pri-focus", StringComparison.Ordinal)))
                {
                    offenders.Add($"{Path.GetFileName(file)}: {call.Value}");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A focus-ring-own() color always resolves, so the ring is never the global one: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void TheFocusRingOwnMixinShouldDrawTheGlobalRingWhileItsColorIsUnset()
    {
        var mixin = SourceFiles.GetScssBlock(SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss").Replace("\r\n", "\n"),
                                             "@mixin focus-ring-own($color, $also: null) {");

        StringAssert.Contains(mixin, "calc(#{$shp-focus-ring-offset} + #{$shp-focus-ring-width}) #{$color};");
        StringAssert.Contains(mixin, "box-shadow: var(--bit-focus-ring-own, var(--bit-shd-focus-ring,");

        // Box-shadows are stripped in forced-colors mode, so the Highlight outline stays on every ring.
        StringAssert.Contains(mixin, "@include focus-ring-forced-colors($shp-focus-ring-offset);");
    }

    private static IEnumerable<(string File, string Stylesheet)> ReadComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories))
            .Select(file => (file, SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n")));
}
