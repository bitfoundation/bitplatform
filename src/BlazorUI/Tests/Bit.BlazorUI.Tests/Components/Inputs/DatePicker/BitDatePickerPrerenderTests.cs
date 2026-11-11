using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DatePicker;

/// <summary>
/// Covers the DatePicker before the page is interactive: a prerender runs the initializing and the parameter hooks
/// with a JS runtime that throws, so the field, its label and the whole calendar - built while initializing - have to
/// come out without a single interop call, which the key setup, the swipe setup, the focus and the callout toggle of
/// an initial IsOpen only make after the first render.
/// </summary>
[TestClass]
public class BitDatePickerPrerenderTests
{
    [TestMethod]
    public async Task BitDatePickerShouldRenderTheFieldAndTheCalendarBeforeInteractivity()
    {
        var html = await Prerenderer.RenderAsync<BitDatePicker>(new Dictionary<string, object?>
        {
            [nameof(BitDatePicker.Label)] = "Birthday",
            [nameof(BitDatePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitDatePicker.TimeZone)] = TimeZoneInfo.Utc,
            [nameof(BitDatePicker.DateFormat)] = "yyyy-MM-dd",
            [nameof(BitDatePicker.Value)] = new DateTimeOffset(new DateTime(2026, 1, 15), TimeSpan.Zero),
            [nameof(BitDatePicker.ShowTimePicker)] = true,
            [nameof(BitDatePicker.ShowWeekNumbers)] = true,
        });

        Assert.Contains("bit-dtp", html);
        Assert.Contains("Birthday", html);
        Assert.Contains("role=\"combobox\"", html);
        Assert.Contains("value=\"2026-01-15\"", html);
        Assert.Contains("bit-dtp-cal", html);
        Assert.Contains("January 2026", html);
        Assert.Contains("bit-dtp-dbs", html);
        Assert.Contains("bit-dtp-wnm", html);
        Assert.Contains("bit-dtp-twp", html);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public async Task BitDatePickerShouldRenderAnInitiallyOpenOrStandalonePickerBeforeInteractivity(bool standalone)
    {
        // An initial IsOpen is applied after the first render, where the JS side exists; a standalone calendar never
        // toggles anything. Neither may reach for the interop while prerendering.
        var html = await Prerenderer.RenderAsync<BitDatePicker>(new Dictionary<string, object?>
        {
            [nameof(BitDatePicker.IsOpen)] = true,
            [nameof(BitDatePicker.AutoFocus)] = true,
            [nameof(BitDatePicker.Responsive)] = true,
            [nameof(BitDatePicker.Standalone)] = standalone,
            [nameof(BitDatePicker.Culture)] = CultureInfo.InvariantCulture,
            [nameof(BitDatePicker.StartingValue)] = new DateTimeOffset(new DateTime(2026, 1, 15), TimeSpan.Zero),
        });

        Assert.Contains("bit-dtp-grd", html);
        Assert.AreEqual(standalone, html.Contains("bit-input-hidden"));
        Assert.AreEqual(standalone, html.Contains("bit-dtp-sta"));
        Assert.AreEqual(standalone is false, html.Contains("bit-dtp-ovl"));
    }
}
