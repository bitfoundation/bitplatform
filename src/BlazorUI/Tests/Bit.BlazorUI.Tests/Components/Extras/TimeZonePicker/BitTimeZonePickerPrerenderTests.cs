using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

/// <summary>
/// Covers what a BitTimeZonePicker sends before the page is interactive (a prerender, or a static SSR page): no
/// OnAfterRender runs, so the browser is never asked for the visitor's time zone, no key listener is wired and no
/// callout is positioned - and the field, its value, its label and the hidden field a form posts are all there.
/// </summary>
[TestClass]
public class BitTimeZonePickerPrerenderTests : BunitTestContext
{
    private static readonly DateTimeOffset January = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly List<BitTimeZone> ThreeTimeZones =
    [
        BitTimeZones.Find("Asia/Tokyo")!,
        BitTimeZones.Find("Asia/Tehran")!,
        BitTimeZones.Find("Europe/London")!,
    ];

    private static async Task<IDocument> PrerenderAsync<TValue>(Action<Dictionary<string, object?>>? configure = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            [nameof(BitTimeZonePicker<TValue>.TimeZones)] = ThreeTimeZones,
            [nameof(BitTimeZonePicker<TValue>.ReferenceDate)] = January,
        };
        configure?.Invoke(parameters);

        var html = await Prerenderer.RenderAsync<BitTimeZonePicker<TValue>>(parameters);

        return new HtmlParser().ParseDocument($"<!DOCTYPE html><html><body>{html}</body></html>");
    }

    [TestMethod]
    public async Task BitTimeZonePickerShouldPrerenderItsValueInTheField()
    {
        var document = await PrerenderAsync<string>(parameters =>
        {
            parameters[nameof(BitTimeZonePicker<string>.Value)] = "Asia/Tehran";
            parameters[nameof(BitTimeZonePicker<string>.Placeholder)] = "Pick one";
        });

        var field = document.QuerySelector("button.bit-tzp-fld")!;

        Assert.AreEqual("combobox", field.GetAttribute("role"));
        Assert.AreEqual("false", field.GetAttribute("aria-expanded"));
        Assert.AreEqual("Tehran", field.QuerySelector(".bit-tzp-txt")!.TextContent);
        Assert.AreEqual("UTC+03:30", field.QuerySelector(".bit-tzp-ofs")!.TextContent);
        Assert.AreEqual("(UTC+03:30) Tehran, Iran", field.GetAttribute("title"));
        Assert.IsNull(field.QuerySelector(".bit-tzp-pls"));

        // The list stays out of the document until the first opening, and the overlay off the page.
        Assert.AreEqual(0, document.QuerySelectorAll("button.bit-tzp-itm").Length);
        StringAssert.Contains(document.QuerySelector(".bit-tzp-ovl")!.GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public async Task BitTimeZonePickerShouldPrerenderTheTimeZoneOfATimeZoneInfoValue()
    {
        var document = await PrerenderAsync<TimeZoneInfo>(parameters =>
        {
            parameters[nameof(BitTimeZonePicker<TimeZoneInfo>.Value)] = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            parameters[nameof(BitTimeZonePicker<TimeZoneInfo>.Name)] = "timeZone";
        });

        Assert.AreEqual("Tokyo", document.QuerySelector(".bit-tzp-txt")!.TextContent);

        // A plain HTML form posts the IANA id.
        var hidden = document.QuerySelector("input[type=hidden]")!;
        Assert.AreEqual("timeZone", hidden.GetAttribute("name"));
        Assert.AreEqual("Asia/Tokyo", hidden.GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTimeZonePickerShouldPrerenderThePlaceholderWithoutAskingTheBrowserForTheTimeZone()
    {
        // Detecting the time zone is JS interop, which a prerender cannot do: the field is left empty rather than
        // filled with the server's time zone, and nothing throws.
        var document = await PrerenderAsync<string>(parameters =>
        {
            parameters[nameof(BitTimeZonePicker<string>.DetectTimeZone)] = true;
            parameters[nameof(BitTimeZonePicker<string>.Placeholder)] = "Pick one";
        });

        Assert.AreEqual("Pick one", document.QuerySelector(".bit-tzp-pls")!.TextContent);
        Assert.IsNull(document.QuerySelector(".bit-tzp-txt"));
    }

    [TestMethod]
    public async Task BitTimeZonePickerShouldPrerenderTheNamesAndDescriptionsOfTheField()
    {
        var document = await PrerenderAsync<string>(parameters =>
        {
            parameters[nameof(BitTimeZonePicker<string>.Label)] = "Display time zone";
            parameters[nameof(BitTimeZonePicker<string>.Description)] = "Dates are shown in it.";
            parameters[nameof(BitTimeZonePicker<string>.ErrorMessage)] = "No office there.";
            parameters[nameof(BitTimeZonePicker<string>.Required)] = true;
        });

        var field = document.QuerySelector("button.bit-tzp-fld")!;
        var label = document.QuerySelector(".bit-tzp-lbl")!;

        Assert.AreEqual("Display time zone", label.TextContent.Trim());
        Assert.AreEqual(label.Id, field.GetAttribute("aria-labelledby"));
        Assert.AreEqual($"{document.QuerySelector(".bit-tzp-erm")!.Id} {document.QuerySelector(".bit-tzp-des")!.Id}", field.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", field.GetAttribute("aria-invalid"));
        Assert.AreEqual("true", field.GetAttribute("aria-required"));
        // The message is announced from the live region as well as shown.
        StringAssert.StartsWith(document.QuerySelector(".bit-tzp-lvr")!.TextContent, "No office there.");
    }

    [TestMethod]
    public async Task BitTimeZonePickerShouldPrerenderADisabledField()
    {
        var document = await PrerenderAsync<string>(parameters =>
        {
            parameters[nameof(BitTimeZonePicker<string>.Disabled)] = true;
        });

        var field = document.QuerySelector("button.bit-tzp-fld")!;

        Assert.IsTrue(document.QuerySelector(".bit-tzp")!.ClassList.Contains("bit-dis"));
        Assert.IsTrue(field.HasAttribute("disabled"));
        Assert.AreEqual("true", field.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public async Task BitTimeZonePickerShouldPrerenderAnOpenListWithItsSelectionMarked()
    {
        var document = await PrerenderAsync<string>(parameters =>
        {
            parameters[nameof(BitTimeZonePicker<string>.Value)] = "Asia/Tehran";
            parameters[nameof(BitTimeZonePicker<string>.IsOpen)] = true;
        });

        // A page that holds the list open gets its rows in the markup, in the order of their offsets, with the selected
        // one marked - the active option and the callout's placement are settled once the browser side is up.
        var field = document.QuerySelector("button.bit-tzp-fld")!;
        Assert.AreEqual("true", field.GetAttribute("aria-expanded"));
        Assert.AreEqual(document.QuerySelector(".bit-tzp-lst")!.Id, field.GetAttribute("aria-controls"));

        var items = document.QuerySelectorAll("button.bit-tzp-itm");
        Assert.AreEqual(3, items.Length);
        Assert.AreEqual("London", items[0].QuerySelector(".bit-tzp-inm")!.TextContent);
        Assert.AreEqual("Tehran", items[1].QuerySelector(".bit-tzp-inm")!.TextContent);
        Assert.AreEqual("Tokyo", items[2].QuerySelector(".bit-tzp-inm")!.TextContent);
        Assert.AreEqual("true", items[1].GetAttribute("aria-selected"));
        Assert.IsTrue(items[1].ClassList.Contains("bit-tzp-sel"));
        Assert.AreEqual("false", items[0].GetAttribute("aria-selected"));
    }
}
