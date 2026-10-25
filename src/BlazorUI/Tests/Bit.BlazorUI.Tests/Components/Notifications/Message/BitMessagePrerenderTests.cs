using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

/// <summary>
/// What a message looks like on a statically rendered page, which has no render after the first one.
/// </summary>
[TestClass]
public class BitMessagePrerenderTests
{
    [TestMethod]
    public async Task BitMessageShouldRenderTheTextOfADelayedAnnouncementOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitMessage>(new Dictionary<string, object?>
        {
            [nameof(BitMessage.DelayedAnnouncement)] = true,
            [nameof(BitMessage.Title)] = "Saved",
            [nameof(BitMessage.IconAriaLabel)] = "Success",
            [nameof(BitMessage.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        // The text a delayed announcement holds back for the next render would otherwise never arrive.
        StringAssert.Contains(html, "Hello");
        StringAssert.Contains(html, "Saved");
        StringAssert.Contains(html, "Success");
    }

    [TestMethod]
    public async Task BitMessageShouldRenderItsTextOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitMessage>(new Dictionary<string, object?>
        {
            [nameof(BitMessage.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Hello")),
        });

        StringAssert.Contains(html, "Hello");
    }
}
