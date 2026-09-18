using System;
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
        // Nothing to correct: the theme is known here, so the OS-resolution script would only be a
        // second script running before every first paint.
        StringAssert.DoesNotMatch(component.Markup, new System.Text.RegularExpressions.Regex("querySelector"),
            "With the theme resolved server-side there is nothing for the correction script to fix.");
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
            .Add(p => p.LightTheme, BitExtraThemePresets.Fluent2Light)
            .Add(p => p.DarkTheme, BitExtraThemePresets.Fluent2Dark)
            .Add(p => p.ThemeColors, BitExtraThemeSurfaces.BackgroundSecondary));

        StringAssert.Contains(component.Markup, BitExtraThemeSurfaces.BackgroundSecondary[BitExtraThemePresets.Fluent2Light], StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(component.Markup, BitExtraThemeSurfaces.BackgroundSecondary[BitExtraThemePresets.Fluent2Dark], StringComparison.OrdinalIgnoreCase);
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
    public void BitThemeHeadShouldResolveTheSameThemeAsTheRootAttributes()
    {
        // The two halves are handed the same preference and must agree: a tag painted for one theme
        // above a document rendering another is a seam on every first paint.
        foreach (var preference in new[] { BitThemePresets.Dark, BitExtraThemePresets.MaterialLight, BitThemePresets.System, null })
        {
            var attributes = BitThemeSsr.BuildRootThemeAttributeMap(preference);
            var component = RenderComponent<BitThemeHead>(parameters => parameters
                .Add(p => p.PersistedPreference, preference)
                .Add(p => p.ThemeColors, BitExtraThemeSurfaces.BackgroundPrimary));

            if (attributes.TryGetValue(BitThemeAttributeNames.Theme, out var theme))
            {
                StringAssert.Contains(component.Markup, BitExtraThemeSurfaces.BackgroundPrimary[(string)theme], StringComparison.OrdinalIgnoreCase,
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
