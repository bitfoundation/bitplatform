using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

/// <summary>
/// What a time picker sends before the page is interactive: the after-render pass that sets its scripts up and applies
/// an initial IsOpen never runs on a prerender, and the renderer it is handed throws on every JS call, so the field, its
/// value and the callout have to come out of the render alone.
/// </summary>
[TestClass]
public class BitTimePickerPrerenderTests
{
    [TestMethod]
    public async Task BitTimePickerShouldRenderTheFieldAndTheCalloutBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitTimePicker>(new Dictionary<string, object?>
        {
            [nameof(BitTimePicker.Label)] = "Meeting time",
            [nameof(BitTimePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitTimePicker.Value)] = new TimeSpan(13, 45, 0),
            [nameof(BitTimePicker.Name)] = "meeting",
            [nameof(BitTimePicker.ShowNowButton)] = true,
            [nameof(BitTimePicker.IsOpen)] = true,
            [nameof(BitTimePicker.Responsive)] = true,
            [nameof(BitTimePicker.AutoFocus)] = true,
        });

        StringAssert.Contains(html, "bit-tpc-inp");
        StringAssert.Contains(html, "value=\"13:45\"");
        StringAssert.Contains(html, "name=\"meeting\"");
        StringAssert.Contains(html, "Meeting time");
        StringAssert.Contains(html, "bit-tpc-cal");
        StringAssert.Contains(html, "aria-expanded=\"true\"");
    }

    [TestMethod]
    public async Task BitTimePickerShouldRenderAStandalonePickerBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitTimePicker>(new Dictionary<string, object?>
        {
            [nameof(BitTimePicker.Standalone)] = true,
            [nameof(BitTimePicker.ShowSeconds)] = true,
            [nameof(BitTimePicker.TimeFormat)] = BitTimeFormat.TwelveHours,
            [nameof(BitTimePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitTimePicker.Value)] = new TimeSpan(13, 45, 30),
        });

        StringAssert.Contains(html, "bit-tpc-sta");
        StringAssert.Contains(html, "role=\"group\"");
        StringAssert.Contains(html, "bit-tpc-am-pm");
        StringAssert.Contains(html, "bit-input-hidden");
        Assert.IsFalse(html.Contains("bit-tpc-ovl"));
    }
}
