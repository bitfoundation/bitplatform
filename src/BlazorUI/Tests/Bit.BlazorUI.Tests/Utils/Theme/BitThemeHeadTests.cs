using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// BitThemeHead is the head half of a host page's theme setup, and what it emits is only ever seen by
/// a browser painting a document for the first time - never by a test that renders the app, and never
/// in a component tree. These cover the part that has to be right before any script or stylesheet
/// exists: which color the browser chrome is painted with, and for which theme.
/// </summary>
[TestClass]
public class BitThemeHeadTests : BunitTestContext
{
    [TestMethod]
    public void BitThemeHeadShouldEmitTheFirstPaintScriptAndTheThemeColorTag()
    {
        var component = RenderComponent<BitThemeHead>();

        StringAssert.Contains(component.Markup, BitThemeSsr.InlineHeadScriptBody, StringComparison.Ordinal,
            "The inline resolution script is the reason this component sits at the top of <head>.");
        StringAssert.Contains(component.Markup, "name=\"theme-color\"", StringComparison.Ordinal);
    }

    [TestMethod]
    public void BitThemeHeadShouldPaintTheChromeWithThePersistedThemesOwnSurface()
    {
        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.PersistedPreference, BitThemePresets.FluentLight));

        StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundPrimary[BitThemePresets.FluentLight], StringComparison.OrdinalIgnoreCase,
            "A visitor with a stored preference is painted that theme, so the chrome must carry that theme's surface.");
        // Still corrected: the head script prefers localStorage over the cookie this response was
        // rendered from, so a resolved theme is not the same as an agreed one.
        StringAssert.Contains(component.Markup, "querySelector", StringComparison.Ordinal,
            "The client can re-resolve to a theme the cookie never mentioned; the tag has to follow it.");
    }

    [TestMethod]
    public void BitThemeHeadShouldCorrectTheTagWhenTheClientResolvesAThemeTheCookieDidNot()
    {
        // The head script reads localStorage first and the cookie only as a fallback, so the theme
        // this response painted can be overruled a moment later. The correction script is what has to
        // carry the color of THAT theme - not of whichever light / dark preset was configured.
        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.PersistedPreference, BitThemePresets.Fluent2Light)
            .Add(p => p.LightTheme, BitThemePresets.Fluent2Light)
            .Add(p => p.DarkTheme, BitThemePresets.Fluent2Dark)
            .Add(p => p.ThemeColors, BitThemeSurfaces.BackgroundSecondary));

        var script = component.Markup[component.Markup.IndexOf("querySelector", StringComparison.Ordinal)..];
        StringAssert.Contains(script, BitThemeSurfaces.BackgroundSecondary[BitThemePresets.MaterialLight], StringComparison.OrdinalIgnoreCase,
            "A stored material-light is a light name the configured light surface does not cover, so it has to be carried by name.");
        StringAssert.Contains(script, BitThemeSurfaces.BackgroundSecondary[BitThemePresets.MaterialDark], StringComparison.OrdinalIgnoreCase);
        // The scheme fallback is a statement of its own rather than the right side of an `||`: in one
        // expression the ternary would swallow the lookup (`a||b?c:d` is `(a||b)?c:d`) and every
        // resolved name would get the light color. The guard is a typeof so a name that reaches
        // Object.prototype - 'constructor', 'toString' - falls through instead of painting a function.
        StringAssert.Contains(script, "if(typeof c!=='string')", StringComparison.Ordinal);
        StringAssert.DoesNotMatch(script, new System.Text.RegularExpressions.Regex(@"\}\[t\]\s*\|\|"),
            "A lookup or-ed straight into the ternary is the precedence trap this shape exists to avoid.");
    }

    [TestMethod]
    public void BitThemeHeadShouldCarryNoLookupTableWhenTheSchemeFallbackAlreadyCoversTheMap()
    {
        // The core family's four names share two colors with the light / dark pair, so naming them
        // would put bytes in front of every first paint that resolve to what the fallback already says.
        // Handed in explicitly rather than left to default: the default is BitThemeSurfaces, a live
        // view over the process-global registry, which Bit.BlazorUI.Extras has put six presets of its
        // own into by the time this runs - so taking the default here would assert nothing.
        var colors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [BitThemePresets.Light] = BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Light],
            [BitThemePresets.Dark] = BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Dark],
            [BitThemePresets.FluentLight] = BitThemeSurfaces.BackgroundPrimary[BitThemePresets.FluentLight],
            [BitThemePresets.FluentDark] = BitThemeSurfaces.BackgroundPrimary[BitThemePresets.FluentDark],
        };

        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.ThemeColors, colors));

        StringAssert.DoesNotMatch(component.Markup, new System.Text.RegularExpressions.Regex(@"\}\[t\]"),
            "No entry of this map needs naming; the scheme fallback resolves all four, so no lookup object is emitted at all.");
        StringAssert.Contains(component.Markup, "var c=/dark$/.test(t)?", StringComparison.Ordinal,
            "What is left is the scheme test alone.");
    }

    [TestMethod]
    public void BitThemeHeadShouldNameThePresetsTheSchemeFallbackWouldGetWrong()
    {
        // The other side of the test above, and the default map's actual case: the registry carries
        // the packaged design systems, whose surfaces are not the core light / dark pair, so those
        // names - and only those - have to be carried by name in front of the fallback.
        var component = RenderComponent<BitThemeHead>();

        StringAssert.Contains(component.Markup, $"'{BitThemePresets.MaterialDark}':'{BitThemeSurfaces.BackgroundPrimary[BitThemePresets.MaterialDark]}'", StringComparison.OrdinalIgnoreCase,
            "material-dark's page surface is not the core dark one, so the fallback would paint the chrome wrong for it.");
        StringAssert.DoesNotMatch(component.Markup, new System.Text.RegularExpressions.Regex($"'{BitThemePresets.FluentDark}':"),
            "fluent-dark shares the core dark surface, so naming it would be dead weight in front of every first paint.");
    }

    [TestMethod]
    public void BitThemeHeadShouldFallBackWithinTheSuppliedMapRatherThanThePackagedSurfaces()
    {
        // An app that hands in its own map has replaced the packaged table on purpose - reaching past
        // it for an unmapped name paints the chrome from a palette the app does not use.
        var colors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["acme-day"] = "#ABCDEF",
            ["acme-dark"] = "#123456",
        };

        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.PersistedPreference, "acme-noon")
            .Add(p => p.ThemeColors, colors));

        StringAssert.Contains(component.Markup, "#ABCDEF", StringComparison.OrdinalIgnoreCase,
            "The only light-side entry of the supplied map, rather than the packaged light surface.");
        StringAssert.DoesNotMatch(component.Markup, new System.Text.RegularExpressions.Regex(BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Light], System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            "The packaged surfaces are the last resort, not the first one past a missing name.");
    }

    [TestMethod]
    public void BitThemeHeadShouldCorrectTheGuessWhenTheVisitorFollowsTheOs()
    {
        // Nothing stored: the server cannot read prefers-color-scheme, so the tag is a guess and the
        // correction script carries the client's answer back before first paint.
        var component = RenderComponent<BitThemeHead>();

        StringAssert.Contains(component.Markup, "querySelector", StringComparison.Ordinal);
        StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Light], StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Dark], StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void BitThemeHeadShouldUseTheConfiguredLightDarkPairForAnOsFollowingVisitor()
    {
        // A Fluent 2 site renames the pair the OS resolution picks between; the chrome has to follow
        // the same rename, or it paints a color from a palette the page never renders.
        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.LightTheme, BitThemePresets.Fluent2Light)
            .Add(p => p.DarkTheme, BitThemePresets.Fluent2Dark)
            .Add(p => p.ThemeColors, BitThemeSurfaces.BackgroundSecondary));

        StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundSecondary[BitThemePresets.Fluent2Light], StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundSecondary[BitThemePresets.Fluent2Dark], StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void BitThemeHeadShouldFallBackToTheSchemeForAThemeTheMapDoesNotCarry()
    {
        // An app's own preset with no color handed in for it: the tag must still land on the right
        // side of light / dark rather than painting the chrome against the page.
        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.PersistedPreference, "acme-dark"));

        StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Dark], StringComparison.OrdinalIgnoreCase,
            "An unknown -dark name resolves to the dark entry, the same way every other layer classifies names.");
    }

    [TestMethod]
    public void BitThemeHeadShouldEmitNoThemeColorWhenTheHostPageWritesItsOwn()
    {
        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.EmitThemeColor, false));

        StringAssert.Contains(component.Markup, BitThemeSsr.InlineHeadScriptBody, StringComparison.Ordinal);
        StringAssert.DoesNotMatch(component.Markup, new System.Text.RegularExpressions.Regex("theme-color"),
            "A host page keeping its own tag (a media-qualified pair, say) must not get a second one.");
    }

    [TestMethod]
    public void BitThemeHeadShouldStampTheNonceOnEveryScriptItEmits()
    {
        // A script-src 'nonce-…' policy blocks an unstamped script, which would leave the theme
        // unresolved and the chrome on its guess - the exact flash this component exists to prevent.
        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.Nonce, "r4nd0m"));

        var scripts = component.Markup.Split("<script", StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, scripts.Length, "Expected the resolution script and the theme-color correction script.");
        foreach (var script in scripts)
        {
            StringAssert.StartsWith(script.TrimStart(), "nonce=\"r4nd0m\"", StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void BitThemeHeadShouldRecognizeADarkThemeWhoseNameDoesNotEndInDark()
    {
        // The suffix rule is what classifies a name everywhere else, but an app is free to call its
        // dark preset anything - and bit-theme then carries that name. Left to the suffix alone the
        // correction script would take the else branch and paint a dark document's chrome with the
        // light surface, which is the one case the whole script exists to prevent.
        var colors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["daylight"] = "#FFFFFF",
        };

        var component = RenderComponent<BitThemeHead>(parameters => parameters
            .Add(p => p.LightTheme, "daylight")
            .Add(p => p.DarkTheme, "midnight")
            .Add(p => p.ThemeColors, colors));

        var script = component.Markup[component.Markup.IndexOf("querySelector", StringComparison.Ordinal)..];

        // By name, since the map carries no 'midnight' entry for the lookup table to name it with.
        StringAssert.Contains(script, "t==='midnight'", StringComparison.Ordinal,
            "The configured dark preset has to be recognized by name, not only by the suffix.");
        // And the suffix stays: the name check covers the configured preset, not every dark name a
        // client may resolve to (an app's own 'acme-dark', a design system's preset).
        StringAssert.Contains(script, "/dark$/.test(t)", StringComparison.Ordinal,
            "The suffix rule is the fallback for every OTHER dark name, so it has to survive.");
        StringAssert.Contains(script, BitThemeSurfaces.BackgroundPrimary[BitThemePresets.Dark], StringComparison.OrdinalIgnoreCase,
            "'midnight' resolves to the dark side, so the dark surface is what that branch paints.");
    }

    [TestMethod]
    public void BitThemeHeadShouldResolveTheSameThemeAsTheRootAttributes()
    {
        // The two halves are handed the same preference and must agree: a tag painted for one theme
        // above a document rendering another is a seam on every first paint.
        foreach (var preference in new[] { BitThemePresets.Dark, BitThemePresets.MaterialLight, BitThemePresets.System, null })
        {
            var attributes = BitThemeSsr.BuildRootThemeAttributeMap(preference);
            var component = RenderComponent<BitThemeHead>(parameters => parameters
                .Add(p => p.PersistedPreference, preference)
                .Add(p => p.ThemeColors, BitThemeSurfaces.BackgroundPrimary));

            if (attributes.TryGetValue(BitThemeAttributeNames.Theme, out var theme))
            {
                StringAssert.Contains(component.Markup, BitThemeSurfaces.BackgroundPrimary[(string)theme], StringComparison.OrdinalIgnoreCase,
                    $"'{preference}' renders {theme}, so the chrome must carry that theme's surface.");
            }
            else
            {
                StringAssert.Contains(component.Markup, "querySelector", StringComparison.Ordinal,
                    $"'{preference}' leaves the theme to the client, so the correction script must be emitted.");
            }
        }
    }
}
