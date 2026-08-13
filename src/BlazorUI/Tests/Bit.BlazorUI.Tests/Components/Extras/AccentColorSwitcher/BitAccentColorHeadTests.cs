using System;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

[TestClass]
public class BitAccentColorHeadTests : BunitTestContext
{
    [TestMethod]
    public void BitAccentColorHeadShouldEmitNothingByDefault()
    {
        var component = RenderComponent<BitAccentColorHead>();

        Assert.AreEqual(string.Empty, component.Markup.Trim(),
            "The default None strategy has no first-paint machinery, so the component must emit nothing - no script, no CSS.");
    }

    [TestMethod]
    public void BitAccentColorHeadShouldFallBackToTheDiRegisteredConfig()
    {
        Context.Services.AddBitBlazorUIExtrasServices(accentColor: config =>
        {
            config.FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss;
            config.Persistence = BitAccentColorPersistence.All;
        });

        var component = RenderComponent<BitAccentColorHead>();

        StringAssert.Contains(component.Markup, BitAccentColorSsr.InlineHeadScriptBody, StringComparison.Ordinal,
            "The DI-registered configuration must reach a parameterless head - stating the config once in shared registration is the whole point.");
        Assert.AreEqual(1, component.FindAll("style").Count);
    }

    [TestMethod]
    public void BitAccentColorHeadInStaticCssModeShouldEmitTheStaticCssButNoScriptWithoutPersistence()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
            });
        });

        Assert.AreEqual(1, component.FindAll("style").Count);
        Assert.IsFalse(component.Markup.Contains("<script", StringComparison.Ordinal),
            "With the default Persistence of None nothing is ever persisted, so there is nothing for an inline script to restore.");
    }

    [TestMethod]
    public void BitAccentColorHeadInStaticCssModeShouldEmitTheInlineScriptAndInlineTheStaticCss()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
        });

        StringAssert.Contains(component.Markup, BitAccentColorSsr.InlineHeadScriptBody, StringComparison.Ordinal,
            "The inline script is what personalizes a cached response pre-paint; without it the component solves nothing.");

        var styles = component.FindAll("style");
        Assert.AreEqual(1, styles.Count);
        StringAssert.Contains(styles[0].TextContent, $"[{BitAccentColorNames.Attribute}=\"8764b8\"]", StringComparison.Ordinal,
            "With no StylesheetHref, the all-accents stylesheet must be inlined so no endpoint is required.");

        Assert.AreEqual(0, component.FindAll("link").Count, "Inlined and linked stylesheets are alternatives, not companions.");
    }

    [TestMethod]
    public void BitAccentColorHeadShouldLinkTheStylesheetWithAVersionCacheBuster()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
            });
            parameters.Add(p => p.StylesheetHref, "accent-colors.css");
        });

        var link = component.Find("link");
        Assert.AreEqual("stylesheet", link.GetAttribute("rel"));
        Assert.AreEqual($"accent-colors.css?v={BitAccentColorSsr.Version}", link.GetAttribute("href"),
            "The library version must ride along as a cache-buster, or an immutable-cached stylesheet outlives a palette-changing release.");

        Assert.AreEqual(0, component.FindAll("style").Count, "A linked stylesheet must not also be inlined.");
    }

    [TestMethod]
    public void BitAccentColorHeadShouldAppendTheVersionWithAnAmpersandWhenTheHrefAlreadyHasAQuery()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
            });
            parameters.Add(p => p.StylesheetHref, "accent-colors.css?tenant=a");
        });

        Assert.AreEqual($"accent-colors.css?tenant=a&v={BitAccentColorSsr.Version}", component.Find("link").GetAttribute("href"));
    }

    [TestMethod]
    public void BitAccentColorHeadShouldEmitTheNonceOnTheInlineScript()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            });
            parameters.Add(p => p.Nonce, "abc123");
        });

        StringAssert.Contains(component.Markup, "<script nonce=\"abc123\">", StringComparison.Ordinal,
            "A CSP-protected host can only run the inline script if the nonce reaches it.");

        Assert.AreEqual("abc123", component.Find("style").GetAttribute("nonce"),
            "The palette stylesheet needs the nonce as much as the script does - a style-src 'nonce-...' policy blocks it otherwise, and the page paints the packaged palette.");
    }

    [TestMethod]
    public void BitAccentColorHeadShouldEmitTheNonceOnThePrerenderStyle()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss,
            });
            parameters.Add(p => p.PersistedAccent, "8764b8");
            parameters.Add(p => p.Nonce, "abc123");
        });

        Assert.AreEqual("abc123", component.Find($"style[id=\"{BitAccentColorNames.StyleElementId}\"]").GetAttribute("nonce"));
    }

    [TestMethod]
    public void BitAccentColorHeadShouldEmitNoNonceAttributeWhenNoNonceIsGiven()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
            });
        });

        Assert.IsFalse(component.Markup.Contains("nonce", StringComparison.Ordinal),
            "Apps without a CSP must not get an empty nonce attribute - an empty nonce is not a wildcard, it is a value that matches nothing.");
    }

    [TestMethod]
    public void BitAccentColorHeadInStoredCssModeShouldEmitThePersistedAccentsPrerenderStyle()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss,
            });
            parameters.Add(p => p.PersistedAccent, "8764b8");
        });

        var style = component.Find($"style[id=\"{BitAccentColorNames.StyleElementId}\"]");
        StringAssert.Contains(style.TextContent, $":root:root[{BitThemeAttributeNames.Theme}$=dark]{{", StringComparison.Ordinal,
            "The per-request style carries the id the runtime client uses to find and drop it, and splits on bit-theme.");

        Assert.AreEqual(0, component.FindAll("link").Count, "StoredCss strategy needs no stylesheet.");
    }

    [DataTestMethod,
        DataRow(null, DisplayName = "No persisted accent"),
        DataRow("1276c6", DisplayName = "The packaged primary needs no override"),
        DataRow("garbage", DisplayName = "A tampered cookie value")]
    public void BitAccentColorHeadInStoredCssModeShouldEmitNoStyleWhenThereIsNothingToOverride(string? persisted)
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StoredCss,
                Persistence = BitAccentColorPersistence.All,
            });
            parameters.Add(p => p.PersistedAccent, persisted);
        });

        Assert.AreEqual(0, component.FindAll("style").Count);
        StringAssert.Contains(component.Markup, BitAccentColorSsr.InlineHeadScriptBody, StringComparison.Ordinal,
            "The inline script must be emitted regardless - it is what restores the accent from the visitor's own stores.");
    }

    [TestMethod]
    public void BitAccentColorHeadShouldHonorACustomAccentList()
    {
        var component = RenderComponent<BitAccentColorHead>(parameters =>
        {
            parameters.Add(p => p.Config, new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Accents = [new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" }],
            });
        });

        var css = component.Find("style").TextContent;
        StringAssert.Contains(css, $"[{BitAccentColorNames.Attribute}=\"dc143c\"]", StringComparison.Ordinal);
        Assert.IsFalse(css.Contains("8764b8", StringComparison.Ordinal),
            "A custom accent list replaces the defaults; the default palettes must not leak in.");
    }
}
