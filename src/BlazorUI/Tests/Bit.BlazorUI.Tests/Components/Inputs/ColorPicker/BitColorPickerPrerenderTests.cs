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

        StringAssert.Contains(html, "bit-clp");
        StringAssert.Contains(html, "Brand color");
        StringAssert.Contains(html, "bit-clp-rec");
        StringAssert.Contains(html, "bit-clp-hsd");
        StringAssert.Contains(html, "bit-clp-asd");
        StringAssert.Contains(html, "bit-clp-pre");
        StringAssert.Contains(html, "bit-clp-cnr");
        StringAssert.Contains(html, "bit-clp-prt");
        // The hexadecimal field and the carrier of the value both show the color the picker was given.
        StringAssert.Contains(html, "value=\"#FF0000FF\"");
        StringAssert.Contains(html, "bit-input-hidden");
        StringAssert.Contains(html, "value=\"#FF0000\"");
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

        StringAssert.Contains(html, "value=\"hsl(0,0%,100%)\"");
        // The eyedropper is a browser capability nothing can ask about while prerendering, so it is not drawn.
        Assert.IsFalse(html.Contains("bit-clp-eyd"));
    }
}
