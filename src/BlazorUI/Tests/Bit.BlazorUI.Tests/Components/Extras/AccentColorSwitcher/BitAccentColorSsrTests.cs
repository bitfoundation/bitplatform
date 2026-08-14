using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

[TestClass]
public sealed class BitAccentColorSsrTests
{
    [TestMethod]
    public void InlineHeadScriptWrapsTheBodyInAScriptElement()
    {
        var script = BitAccentColorSsr.InlineHeadScript;

        StringAssert.StartsWith(script, "<script>", StringComparison.Ordinal,
            "InlineHeadScript must be drop-in <head> markup, so it has to carry its own script element.");
        StringAssert.EndsWith(script, "</script>", StringComparison.Ordinal);
        StringAssert.Contains(script, BitAccentColorSsr.InlineHeadScriptBody, StringComparison.Ordinal,
            "The wrapped body and the raw body must be the same script, or the two APIs drift apart.");
    }

    [TestMethod]
    public void BuildInlineHeadScriptWithoutNonceEqualsTheDefaultProperty()
    {
        Assert.AreEqual(BitAccentColorSsr.InlineHeadScript, BitAccentColorSsr.BuildInlineHeadScript(null));
        Assert.AreEqual(BitAccentColorSsr.InlineHeadScript, BitAccentColorSsr.BuildInlineHeadScript(string.Empty));
        Assert.AreEqual(BitAccentColorSsr.InlineHeadScript, BitAccentColorSsr.BuildInlineHeadScript("   "));
    }

    [TestMethod]
    public void BuildInlineHeadScriptEmitsAnEncodedNonceAttribute()
    {
        var script = BitAccentColorSsr.BuildInlineHeadScript("abc123");
        StringAssert.StartsWith(script, "<script nonce=\"abc123\">", StringComparison.Ordinal);

        // A tampered nonce must not be able to break out of the attribute context.
        var tampered = BitAccentColorSsr.BuildInlineHeadScript("x\"><script>alert(1)</script>");
        StringAssert.Contains(tampered, "nonce=\"x&quot;&gt;&lt;script&gt;", StringComparison.Ordinal,
            "The nonce must be HTML-attribute-encoded so it cannot inject markup.");
    }

    [TestMethod]
    public void InlineHeadScriptBodyReferencesTheNamesFromBitAccentColorNames()
    {
        // The script and BitAccentColor.ts read the same attribute and stores; sourcing the names
        // from BitAccentColorNames is what keeps a rename from silently breaking persistence.
        var body = BitAccentColorSsr.InlineHeadScriptBody;

        StringAssert.Contains(body, $"'{BitAccentColorNames.StorageKey}'", StringComparison.Ordinal);
        StringAssert.Contains(body, $"'{BitAccentColorNames.CookieName}'", StringComparison.Ordinal);
        StringAssert.Contains(body, $"'{BitAccentColorNames.CssStorageKey}'", StringComparison.Ordinal);
        StringAssert.Contains(body, $"'{BitAccentColorNames.Attribute}'", StringComparison.Ordinal);
        StringAssert.Contains(body, $"'{BitAccentColorNames.StyleElementId}'", StringComparison.Ordinal);
    }

    [DataTestMethod,
        DataRow("#8764B8", "bit-accent=\"8764b8\"", DisplayName = "A #-prefixed hex is stripped and lower-cased"),
        DataRow("8764b8", "bit-accent=\"8764b8\"", DisplayName = "A bare hex token passes through"),
        DataRow("  #CA5010  ", "bit-accent=\"ca5010\"", DisplayName = "Whitespace is trimmed"),
        DataRow("abc", "bit-accent=\"abc\"", DisplayName = "A 3-digit hex is valid"),
        DataRow(null, "", DisplayName = "Null means no preference"),
        DataRow("", "", DisplayName = "Blank means no preference"),
        DataRow("not-a-color", "", DisplayName = "A non-hex value is rejected"),
        DataRow("8764b", "", DisplayName = "A wrong-length hex is rejected"),
        DataRow("\"><script>alert(1)</script>", "", DisplayName = "Markup injection is rejected"),
        DataRow("8764b8\" onload=\"x", "", DisplayName = "Attribute breakout is rejected")]
    public void BuildRootAccentAttributesValidatesThePersistedValue(string? persisted, string expected)
    {
        // The cookie is visitor-editable, so anything that is not a plain hex token must be treated
        // as "nothing stored" rather than emitted into the document.
        Assert.AreEqual(expected, BitAccentColorSsr.BuildRootAccentAttributes(persisted));
    }

    [DataTestMethod,
        DataRow("#8764B8", DisplayName = "Stored accent"),
        DataRow(null, DisplayName = "No stored accent"),
        DataRow("garbage", DisplayName = "Tampered accent")]
    public void BuildRootAccentAttributeMapMatchesTheStringOverload(string? persisted)
    {
        var text = BitAccentColorSsr.BuildRootAccentAttributes(persisted);
        var map = BitAccentColorSsr.BuildRootAccentAttributeMap(persisted);

        if (text.Length == 0)
        {
            Assert.AreEqual(0, map.Count, "Both overloads are built from the same resolution, so they cannot disagree on emptiness.");
        }
        else
        {
            Assert.AreEqual(1, map.Count);
            var token = map[BitAccentColorNames.Attribute] as string;
            Assert.AreEqual(text, $"{BitAccentColorNames.Attribute}=\"{token}\"",
                "Both overloads are built from the same resolution, so they cannot drift apart.");
        }
    }

    [TestMethod]
    public void BuildStaticCssScopesEveryNonDefaultAccentAndSkipsThePackagedPrimary()
    {
        var css = BitAccentColorSsr.BuildStaticCss();

        foreach (var item in BitAccentColorSwitcher.DefaultAccents)
        {
            var token = item.Color.TrimStart('#').ToLowerInvariant();
            var darkScope = $":root:root[{BitAccentColorNames.Attribute}=\"{token}\"][{BitThemeAttributeNames.Theme}$=dark]";
            var lightScope = $":root:root[{BitAccentColorNames.Attribute}=\"{token}\"]:not([{BitThemeAttributeNames.Theme}$=dark])";

            if (string.Equals(item.Color, BitAccentColorPresets.Blue, StringComparison.OrdinalIgnoreCase))
            {
                // Blue is the packaged palette's own primary: emitting rules for it would only
                // bloat the stylesheet to repaint what the packaged CSS already paints.
                Assert.IsFalse(css.Contains(darkScope, StringComparison.Ordinal), "The packaged primary needs no override.");
            }
            else
            {
                StringAssert.Contains(css, darkScope, StringComparison.Ordinal,
                    $"The {item.Name} accent must have a dark-scheme rule, or dark visitors flash the packaged palette.");
                StringAssert.Contains(css, lightScope, StringComparison.Ordinal,
                    $"The {item.Name} accent must have a light-scheme rule, or light visitors flash the packaged palette.");
            }
        }
    }

    [TestMethod]
    public void BuildStaticCssHonorsACustomAccentList()
    {
        var css = BitAccentColorSsr.BuildStaticCss([new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" }]);

        StringAssert.Contains(css, $"[{BitAccentColorNames.Attribute}=\"dc143c\"]", StringComparison.Ordinal);
        Assert.IsFalse(css.Contains("8764b8", StringComparison.Ordinal),
            "A custom accent list replaces the defaults; the default palettes must not leak in.");
    }

    [TestMethod]
    public void BuildSwatchMarkerCssRingsTheSwatchTheRootAttributeNames()
    {
        var css = BitAccentColorSsr.BuildSwatchMarkerCss();

        var purpleToken = BitAccentColorPresets.Purple.TrimStart('#').ToLowerInvariant();
        StringAssert.Contains(css, $":root[{BitAccentColorNames.Attribute}=\"{purpleToken}\"] [{BitAccentColorNames.SwatchAttribute}=\"{purpleToken}\"]", StringComparison.Ordinal,
            "The ring must key on the attribute the inline head script sets pre-paint, and on the token the swatch carries.");

        var blueToken = BitAccentColorPresets.Blue.TrimStart('#').ToLowerInvariant();
        StringAssert.Contains(css, $":root:not([{BitAccentColorNames.Attribute}]) [{BitAccentColorNames.SwatchAttribute}=\"{blueToken}\"]", StringComparison.Ordinal,
            "With no bit-accent attribute set (no override), the packaged primary's swatch must ring.");

        // The declarations mirror .bit-acs-act, which is what marks the swatch once the C# state
        // takes over at hydration - a different ring would visibly change at that moment.
        StringAssert.Contains(css, "outline:0.125rem solid var(--bit-acs-clr)", StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildSwatchMarkerCssIsNotScopedToASwitcherInstance()
    {
        // A swatch is ringed exactly when its own token is the active accent, which holds for every
        // instance offering that accent - so one rule set emitted once covers them all.
        Assert.IsFalse(BitAccentColorSsr.BuildSwatchMarkerCss().Contains("[id=", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildStaticCssAcceptsAnAccentSpelledWithoutTheHash()
    {
        // The accents are validated with NormalizeToken, which accepts a bare token, so an Accents
        // entry may legitimately carry one - and this runs while the host page's <head> is being
        // rendered, where an ArgumentException from the palette factory takes the whole page down.
        var css = BitAccentColorSsr.BuildStaticCss([new BitAccentColorItem { Name = "Crimson", Color = "DC143C" }]);

        StringAssert.Contains(css, $"[{BitAccentColorNames.Attribute}=\"dc143c\"]", StringComparison.Ordinal);
        StringAssert.Contains(css, "--bit-clr-pri:", StringComparison.Ordinal, "The palette must actually be derived, not just scoped.");
    }

    [TestMethod]
    public void BuildStaticCssSkipsAnAccentThatIsNotHexAtAll()
    {
        var css = BitAccentColorSsr.BuildStaticCss(
        [
            new BitAccentColorItem { Name = "Nonsense", Color = "rebeccapurple" },
            new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" },
        ]);

        StringAssert.Contains(css, $"[{BitAccentColorNames.Attribute}=\"dc143c\"]", StringComparison.Ordinal,
            "One unusable entry must not cost the visitor the accents that are usable.");
        Assert.IsFalse(css.Contains("rebeccapurple", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildPrerenderCssAcceptsAnAccentSpelledWithoutTheHash()
    {
        var css = BitAccentColorSsr.BuildPrerenderCss("dc143c", [new BitAccentColorItem { Name = "Crimson", Color = "DC143C" }]);

        Assert.IsNotNull(css, "A bare-token accent is offered like any other, so the per-request style has to derive it.");
        StringAssert.Contains(css, "--bit-clr-pri:", StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildPrerenderCssReturnsNullWhenThereIsNothingToOverride()
    {
        Assert.IsNull(BitAccentColorSsr.BuildPrerenderCss(null), "No stored accent means no override.");
        Assert.IsNull(BitAccentColorSsr.BuildPrerenderCss(BitAccentColorPresets.Blue), "The packaged primary needs no override.");
        Assert.IsNull(BitAccentColorSsr.BuildPrerenderCss("112233"),
            "A value outside the offered accents is visitor-tampered or stale and must not trigger palette derivation.");
    }

    [TestMethod]
    public void BuildPrerenderCssEmitsBothSchemesSplitOnTheThemeAttribute()
    {
        var css = BitAccentColorSsr.BuildPrerenderCss(BitAccentColorPresets.Purple);

        Assert.IsNotNull(css);
        StringAssert.Contains(css, $":root:root[{BitThemeAttributeNames.Theme}$=dark]{{", StringComparison.Ordinal,
            "The dark rule must key on the same bit-theme attribute the theme scripts set.");
        StringAssert.Contains(css, $":root:root:not([{BitThemeAttributeNames.Theme}$=dark]){{", StringComparison.Ordinal,
            "The light rule must cover every non-dark theme, or a custom-named theme paints unaccented.");
        StringAssert.Contains(css, "--bit-clr-pri:", StringComparison.Ordinal,
            "The palette must actually carry the derived tokens.");
    }

    [TestMethod]
    public void PrerenderCssGuardScriptTargetsOnlyTheServerEmittedStyle()
    {
        var script = BitAccentColorSsr.PrerenderCssGuardScript;

        StringAssert.StartsWith(script, "<script>", StringComparison.Ordinal);
        StringAssert.EndsWith(script, "</script>", StringComparison.Ordinal);

        StringAssert.Contains(script, $"getAttribute('{BitAccentColorNames.Attribute}')", StringComparison.Ordinal,
            "The guard compares against the accent the inline head script resolved onto the root element.");
        StringAssert.Contains(script, $"style[id=\"{BitAccentColorNames.StyleElementId}\"][{BitAccentColorNames.StyleAccentAttribute}]", StringComparison.Ordinal,
            "Only the marked (server-emitted) style may be dropped; the snapshot the inline head script injects carries the accent it just resolved.");
    }

    [TestMethod]
    public void BuildPrerenderCssGuardScriptEmitsAnEncodedNonceAttribute()
    {
        Assert.AreEqual(BitAccentColorSsr.PrerenderCssGuardScript, BitAccentColorSsr.BuildPrerenderCssGuardScript(null));

        StringAssert.StartsWith(BitAccentColorSsr.BuildPrerenderCssGuardScript("abc123"), "<script nonce=\"abc123\">", StringComparison.Ordinal,
            "The guard runs under the same CSP as the inline head script, so it needs the nonce as much.");

        StringAssert.Contains(BitAccentColorSsr.BuildPrerenderCssGuardScript("x\"><script>alert(1)</script>"), "nonce=\"x&quot;&gt;&lt;script&gt;", StringComparison.Ordinal,
            "The nonce must be HTML-attribute-encoded so it cannot inject markup.");
    }

    [TestMethod]
    public void BuildPrerenderCssValidatesAgainstTheProvidedAccents()
    {
        var accents = new[] { new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" } };

        Assert.IsNotNull(BitAccentColorSsr.BuildPrerenderCss("dc143c", accents));
        Assert.IsNull(BitAccentColorSsr.BuildPrerenderCss(BitAccentColorPresets.Purple, accents),
            "With a custom accent list, the defaults are not offered and must not validate.");
    }

    [TestMethod]
    public void DefaultAccentsAreTheSixPresetHues()
    {
        CollectionAssert.AreEqual(
            new[] { BitAccentColorPresets.Blue, BitAccentColorPresets.Purple, BitAccentColorPresets.Green, BitAccentColorPresets.Orange, BitAccentColorPresets.Teal, BitAccentColorPresets.Rose },
            BitAccentColorSwitcher.DefaultAccents.Select(a => a.Color).ToArray(),
            "The switcher's defaults are pinned to BitAccentColorPresets so the packaged palette (Blue first, i.e. \"no override\") stays the neutral swatch.");
    }
}
