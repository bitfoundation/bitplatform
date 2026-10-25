using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.SnackBar;

/// <summary>
/// What a snack bar host looks like on a statically rendered page, which has no JS runtime behind it and no render
/// after the first one: the hotkey, the swipe and the focus hand-off it registers through script must not keep the
/// host from rendering at all.
/// </summary>
[TestClass]
public class BitSnackBarPrerenderTests
{
    [TestMethod]
    public async Task BitSnackBarShouldRenderItsHostOnAStaticPage()
    {
        var html = await Prerenderer.RenderAsync<BitSnackBar>(new Dictionary<string, object?>
        {
            [nameof(BitSnackBar.ServiceHost)] = true,
            [nameof(BitSnackBar.Hotkey)] = new[] { "F8" },
            [nameof(BitSnackBar.SwipeToDismiss)] = true,
            [nameof(BitSnackBar.ClearOnNavigation)] = true,
            [nameof(BitSnackBar.Position)] = BitPosition.TopCenter,
        });

        // An empty host is still on the page, where the items shown once the page is interactive will land.
        StringAssert.Contains(html, "bit-snb");
        StringAssert.Contains(html, "bit-snb-tcn");
    }
}
