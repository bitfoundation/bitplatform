using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

/// <summary>
/// What the switcher and the head send before the page is interactive: the restore runs from the after-render pass,
/// which a prerender never reaches, so the markup has to be right with the C# accent state being the server's - and
/// under a CSS strategy that state may be another visitor's, which is why the marking is left to the head's CSS.
/// </summary>
[TestClass]
public class BitAccentColorSwitcherPrerenderTests
{
    private static readonly string PurpleToken = BitAccentColorPresets.Purple.TrimStart('#').ToLowerInvariant();

    [TestMethod]
    public async Task BitAccentColorSwitcherShouldMarkTheNeutralSwatchByClassBeforeInteractivityWithoutAFirstPaintStrategy()
    {
        var html = await Prerenderer.RenderAsync<BitAccentColorSwitcher>();

        // No first-paint machinery, so nothing can ring a swatch but the class - and the server's state is the
        // packaged primary.
        StringAssert.Contains(html, "bit-acs-act");
        Assert.IsFalse(html.Contains(BitAccentColorNames.SwatchAttribute), "Without the marker CSS a token on the swatch would key on nothing.");
        StringAssert.Contains(html, "aria-pressed=\"true\"");
    }

    [TestMethod]
    public async Task BitAccentColorSwitcherShouldLeaveTheMarkingToTheHeadCssBeforeInteractivityUnderACssStrategy()
    {
        var html = await Prerenderer.RenderAsync<BitAccentColorSwitcher>(new Dictionary<string, object?>
        {
            [nameof(BitAccentColorSwitcher.Config)] = new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            },
        });

        // A cached response reaches visitors whose accent the server never saw, so the class marks nothing and
        // every swatch carries its token for the marker CSS, keyed on the bit-accent root attribute, to ring.
        Assert.IsFalse(html.Contains("bit-acs-act"));
        StringAssert.Contains(html, $"{BitAccentColorNames.SwatchAttribute}=\"{PurpleToken}\"");
    }

    [TestMethod]
    public async Task BitAccentColorSwitcherShouldRenderTheSwatchesWithoutJsBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccentColorSwitcher>(new Dictionary<string, object?>
        {
            [nameof(BitAccentColorSwitcher.Config)] = new BitAccentColorConfig
            {
                Accents = [new BitAccentColorItem { Name = "Crimson", Color = "#DC143C" }],
            },
        });

        StringAssert.Contains(html, "role=\"group\"");
        StringAssert.Contains(html, "title=\"Crimson\"");
        StringAssert.Contains(html, "--bit-acs-clr:#DC143C");
    }

    [TestMethod]
    public async Task BitAccentColorHeadShouldEmitTheFirstPaintMachineryBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitAccentColorHead>(new Dictionary<string, object?>
        {
            [nameof(BitAccentColorHead.Config)] = new BitAccentColorConfig
            {
                FirstPaintStrategy = BitAccentColorFirstPaintStrategy.StaticCss,
                Persistence = BitAccentColorPersistence.All,
            },
        });

        // The head is only ever rendered this way - it lives in the host page - so this is the one render that matters.
        StringAssert.Contains(html, BitAccentColorSsr.InlineHeadScriptBody);
        StringAssert.Contains(html, $"[{BitAccentColorNames.Attribute}=\"{PurpleToken}\"]");
        StringAssert.Contains(html, $"[{BitAccentColorNames.SwatchAttribute}=\"{PurpleToken}\"]");
    }
}
