using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

/// <summary>
/// Covers the DateRangePicker before the page is interactive: a prerender runs the initializing and the parameter
/// hooks with a JS runtime that throws, so the field, its label, the presets and the whole calendar - built while
/// initializing - have to come out without a single interop call, which the key setup, the swipe setup, the focus and
/// the callout toggle of an initial IsOpen only make after the first render.
/// </summary>
[TestClass]
public class BitDateRangePickerPrerenderTests
{
    [TestMethod]
    public async Task BitDateRangePickerShouldRenderTheFieldAndTheCalendarBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitDateRangePicker>(new Dictionary<string, object?>
        {
            [nameof(BitDateRangePicker.Label)] = "Stay",
            [nameof(BitDateRangePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitDateRangePicker.TimeZone)] = TimeZoneInfo.Utc,
            [nameof(BitDateRangePicker.DateFormat)] = "yyyy-MM-dd",
            [nameof(BitDateRangePicker.ValueFormat)] = "{0} - {1}",
            [nameof(BitDateRangePicker.Value)] = new BitDateRangePickerValue
            {
                StartDate = new DateTimeOffset(new DateTime(2026, 1, 15), TimeSpan.Zero),
                EndDate = new DateTimeOffset(new DateTime(2026, 1, 20), TimeSpan.Zero),
            },
            [nameof(BitDateRangePicker.Presets)] = new[]
            {
                new BitDateRangePickerPreset { Text = "This week", Value = new() { StartDate = new DateTimeOffset(new DateTime(2026, 1, 12), TimeSpan.Zero), EndDate = new DateTimeOffset(new DateTime(2026, 1, 18), TimeSpan.Zero) } },
            },
        });

        StringAssert.Contains(html, "bit-dtrp");
        StringAssert.Contains(html, "Stay");
        StringAssert.Contains(html, "role=\"combobox\"");
        StringAssert.Contains(html, "value=\"2026-01-15 - 2026-01-20\"");
        StringAssert.Contains(html, "bit-dtrp-cal");
        StringAssert.Contains(html, "January 2026");
        StringAssert.Contains(html, "bit-dtrp-dss");
        StringAssert.Contains(html, "bit-dtrp-dse");
        StringAssert.Contains(html, "bit-dtrp-dsb");
        StringAssert.Contains(html, "This week");
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public async Task BitDateRangePickerShouldRenderAnInitiallyOpenOrStandalonePickerBeforeInteractivity(bool standalone)
    {
        // An initial IsOpen is applied after the first render, where the JS side exists; a standalone calendar never
        // toggles anything. Neither may reach for the interop while prerendering.
        var html = await Prerenderer.RenderAsync<BitDateRangePicker>(new Dictionary<string, object?>
        {
            [nameof(BitDateRangePicker.IsOpen)] = true,
            [nameof(BitDateRangePicker.AutoFocus)] = true,
            [nameof(BitDateRangePicker.Responsive)] = true,
            [nameof(BitDateRangePicker.AutoApply)] = false,
            [nameof(BitDateRangePicker.Standalone)] = standalone,
            [nameof(BitDateRangePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitDateRangePicker.StartingValue)] = new BitDateRangePickerValue { StartDate = new DateTimeOffset(new DateTime(2026, 1, 15), TimeSpan.Zero) },
        });

        StringAssert.Contains(html, "bit-dtrp-grd");
        Assert.AreEqual(standalone, html.Contains("bit-input-hidden"));
        Assert.AreEqual(standalone, html.Contains("bit-dtrp-sta"));
        Assert.AreEqual(standalone is false, html.Contains("bit-dtrp-ovl"));
        // The Apply and Cancel buttons are the callout's own, which a standalone calendar has none of.
        Assert.AreEqual(standalone is false, html.Contains("bit-dtrp-act"));
    }
}
