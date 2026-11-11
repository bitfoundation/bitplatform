using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

/// <summary>
/// Covers the ColorPicker before the page is interactive: a prerender runs the initializing and the parameter hooks
/// with a JS runtime that throws, so the color has to be parsed, formatted and drawn - the gradient, the sliders, the
/// fields and the carrier of the value - without a single interop call, which the drag listeners, the eyedropper
/// check and the focus only make after the first render.
/// </summary>
[TestClass]
public class BitColorPickerPrerenderTests
{
    [TestMethod]
    public async Task BitColorPickerShouldRenderItsPanelBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitColorPicker>(new Dictionary<string, object?>
        {
            [nameof(BitColorPicker.Value)] = "#FF0000",
            [nameof(BitColorPicker.Label)] = "Brand color",
            [nameof(BitColorPicker.ShowInputs)] = true,
            [nameof(BitColorPicker.ShowAlphaSlider)] = true,
            [nameof(BitColorPicker.ShowPreview)] = true,
            [nameof(BitColorPicker.ShowContrast)] = true,
            [nameof(BitColorPicker.Presets)] = new[] { "#FF0000", "#00FF00" },
        });

        Assert.Contains("bit-clp", html);
        Assert.Contains("Brand color", html);
        Assert.Contains("bit-clp-rec", html);
        Assert.Contains("bit-clp-hsd", html);
        Assert.Contains("bit-clp-asd", html);
        Assert.Contains("bit-clp-pre", html);
        Assert.Contains("bit-clp-cnr", html);
        Assert.Contains("bit-clp-prt", html);
        // The hexadecimal field and the carrier of the value both show the color the picker was given.
        Assert.Contains("value=\"#FF0000FF\"", html);
        Assert.Contains("bit-input-hidden", html);
        Assert.Contains("value=\"#FF0000\"", html);
    }

    [TestMethod]
    public async Task BitColorPickerShouldWriteTheRequestedFormatBeforeInteractivity()
    {
        // An uncontrolled picker reports its color in the requested notation from the very first render, which is
        // the render a prerender hands the visitor.
        var html = await Prerenderer.RenderAsync<BitColorPicker>(new Dictionary<string, object?>
        {
            [nameof(BitColorPicker.Format)] = BitColorFormat.Hsl,
            [nameof(BitColorPicker.ShowEyeDropper)] = true,
        });

        Assert.Contains("value=\"hsl(0,0%,100%)\"", html);
        // The eyedropper is a browser capability nothing can ask about while prerendering, so it is not drawn.
        Assert.DoesNotContain("bit-clp-eyd", html);
    }
}
