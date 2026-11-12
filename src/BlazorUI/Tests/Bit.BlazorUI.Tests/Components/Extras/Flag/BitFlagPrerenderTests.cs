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

        Assert.Contains("_content/Bit.BlazorUI.Extras/flags/NL-flat-16.webp", html);
        Assert.Contains("alt=\"Netherlands\"", html);
        Assert.Contains("bit-flg-rnd", html);
    }

    [TestMethod]
    public async Task BitFlagShouldPrerenderTheEmojiFlag()
    {
        var html = await Prerenderer.RenderAsync<BitFlag>(new Dictionary<string, object?>
        {
            [nameof(BitFlag.Iso2)] = "nl",
            [nameof(BitFlag.Emoji)] = true,
        });

        Assert.Contains("bit-flg-emj", html);
        // The renderer writes the two regional indicators as character references.
        Assert.Contains("\U0001F1F3\U0001F1F1", System.Net.WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("<img", html);
    }

    [TestMethod]
    public async Task BitFlagShouldPrerenderAClickableFlagWithoutAskingTheBrowserForAnything()
    {
        var html = await Prerenderer.RenderAsync<BitFlag>(new Dictionary<string, object?>
        {
            [nameof(BitFlag.Iso2)] = "nl",
            [nameof(BitFlag.OnClick)] = EventCallback.Factory.Create<MouseEventArgs>(new object(), () => { }),
        });

        Assert.Contains("role=\"button\"", html);
        Assert.Contains("tabindex=\"0\"", html);
        Assert.Contains("aria-label=\"Netherlands\"", html);
    }
}
