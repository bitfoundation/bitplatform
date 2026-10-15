using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Flag;

/// <summary>
/// Covers what a BitFlag sends before the page is interactive (a prerender, or a static SSR page): the flag is drawn
/// from its parameters alone, and the browser-side listener a clickable flag installs waits for the interactive
/// render rather than failing the prerender with a JS call.
/// </summary>
[TestClass]
public class BitFlagPrerenderTests
{
    [TestMethod]
    public async Task BitFlagShouldPrerenderThePackagedImage()
    {
        var html = await Prerenderer.RenderAsync<BitFlag>(new Dictionary<string, object?>
        {
            [nameof(BitFlag.Iso2)] = "nl",
            [nameof(BitFlag.AutoAlt)] = true,
            [nameof(BitFlag.Rounded)] = true,
        });

        StringAssert.Contains(html, "_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp");
        StringAssert.Contains(html, "alt=\"Netherlands\"");
        StringAssert.Contains(html, "bit-flg-rnd");
    }

    [TestMethod]
    public async Task BitFlagShouldPrerenderTheEmojiFlag()
    {
        var html = await Prerenderer.RenderAsync<BitFlag>(new Dictionary<string, object?>
        {
            [nameof(BitFlag.Iso2)] = "nl",
            [nameof(BitFlag.Emoji)] = true,
        });

        StringAssert.Contains(html, "bit-flg-emj");
        // The renderer writes the two regional indicators as character references.
        StringAssert.Contains(System.Net.WebUtility.HtmlDecode(html), "\U0001F1F3\U0001F1F1");
        Assert.IsFalse(html.Contains("<img"));
    }

    [TestMethod]
    public async Task BitFlagShouldPrerenderAClickableFlagWithoutAskingTheBrowserForAnything()
    {
        var html = await Prerenderer.RenderAsync<BitFlag>(new Dictionary<string, object?>
        {
            [nameof(BitFlag.Iso2)] = "nl",
            [nameof(BitFlag.OnClick)] = EventCallback.Factory.Create<MouseEventArgs>(new object(), () => { }),
        });

        StringAssert.Contains(html, "role=\"button\"");
        StringAssert.Contains(html, "tabindex=\"0\"");
        StringAssert.Contains(html, "aria-label=\"Netherlands\"");
    }
}
