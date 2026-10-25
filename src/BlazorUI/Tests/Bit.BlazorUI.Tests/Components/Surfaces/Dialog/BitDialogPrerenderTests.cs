using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

/// <summary>
/// What a dialog sends before the page is interactive: the after-render pass never runs on a prerender, so an open
/// dialog is drawn from the render alone, with none of the focus, scroll and drag plumbing reaching a JS side that
/// a prerender does not have.
/// </summary>
[TestClass]
public class BitDialogPrerenderTests
{
    [TestMethod]
    public async Task BitDialogShouldRenderAnOpenDialogBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitDialog>(new Dictionary<string, object?>
        {
            [nameof(BitDialog.IsOpen)] = true,
            [nameof(BitDialog.Title)] = "Delete the file?",
            [nameof(BitDialog.Message)] = "This cannot be undone.",
        });

        StringAssert.Contains(html, "bit-dlg");
        StringAssert.Contains(html, "role=\"dialog\"");
        StringAssert.Contains(html, "aria-modal=\"true\"");
        StringAssert.Contains(html, "Delete the file?");
        StringAssert.Contains(html, "This cannot be undone.");
        StringAssert.Contains(html, "bit-dlg-okb");
        StringAssert.Contains(html, "bit-dlg-cnb");
    }

    [TestMethod]
    public async Task BitDialogShouldRenderNothingForAClosedDialogBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitDialog>(new Dictionary<string, object?>
        {
            [nameof(BitDialog.IsOpen)] = false,
            [nameof(BitDialog.KeepMounted)] = true,
            [nameof(BitDialog.Title)] = "Delete the file?",
        });

        // A kept-mounted dialog that has never opened is not rendered at all, so a page of them costs nothing.
        Assert.IsFalse(html.Contains("bit-dlg"));
    }

    [TestMethod]
    public async Task BitDialogShouldRenderTheUncontrolledStartingStateBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitDialog>(new Dictionary<string, object?>
        {
            [nameof(BitDialog.DefaultIsOpen)] = true,
            [nameof(BitDialog.Title)] = "Welcome",
        });

        StringAssert.Contains(html, "bit-dlg-ctn");
        StringAssert.Contains(html, "Welcome");
    }
}
