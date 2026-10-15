using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CountryPicker;

/// <summary>
/// Covers what a BitCountryPicker sends before the page is interactive (a prerender, or a static SSR page): no
/// OnAfterRender runs, so no key listener is wired and no callout is positioned - and the field, its value, its flag,
/// its label and the hidden field a form posts are all there, while the list stays out of the document.
/// </summary>
[TestClass]
public class BitCountryPickerPrerenderTests
{
    private static readonly List<BitCountry> ThreeCountries =
    [
        BitCountries.Germany,
        BitCountries.France,
        BitCountries.Japan,
    ];

    private static async Task<IDocument> PrerenderAsync(Action<Dictionary<string, object?>>? configure = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            [nameof(BitCountryPicker.Countries)] = ThreeCountries,
        };
        configure?.Invoke(parameters);

        var html = await Prerenderer.RenderAsync<BitCountryPicker>(parameters);

        return new HtmlParser().ParseDocument($"<!DOCTYPE html><html><body>{html}</body></html>");
    }

    [TestMethod]
    public async Task BitCountryPickerShouldPrerenderItsValueInTheField()
    {
        var document = await PrerenderAsync(parameters =>
        {
            parameters[nameof(BitCountryPicker.Value)] = BitCountries.France;
            parameters[nameof(BitCountryPicker.Placeholder)] = "Pick one";
        });

        var field = document.QuerySelector("button.bit-cnp-fld")!;

        Assert.AreEqual("combobox", field.GetAttribute("role"));
        Assert.AreEqual("false", field.GetAttribute("aria-expanded"));
        Assert.AreEqual("France", field.QuerySelector(".bit-cnp-txt")!.TextContent);
        Assert.IsNotNull(field.QuerySelector(".bit-cnp-flg"));
        Assert.IsNull(field.QuerySelector(".bit-cnp-pls"));

        // The list stays out of the document until the first opening, and the overlay off the page.
        Assert.AreEqual(0, document.QuerySelectorAll("button.bit-cnp-itm").Length);
        StringAssert.Contains(document.QuerySelector(".bit-cnp-ovl")!.GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public async Task BitCountryPickerShouldPrerenderThePlaceholderWithoutASelection()
    {
        var document = await PrerenderAsync(parameters =>
        {
            parameters[nameof(BitCountryPicker.Placeholder)] = "Pick one";
        });

        Assert.AreEqual("Pick one", document.QuerySelector(".bit-cnp-pls")!.TextContent);
        Assert.IsNull(document.QuerySelector(".bit-cnp-txt"));
    }

    [TestMethod]
    public async Task BitCountryPickerShouldPrerenderTheDefaultCountryAndTheHiddenField()
    {
        var document = await PrerenderAsync(parameters =>
        {
            parameters[nameof(BitCountryPicker.DefaultCountry)] = BitCountries.Japan;
            parameters[nameof(BitCountryPicker.Name)] = "country";
        });

        Assert.AreEqual("Japan", document.QuerySelector(".bit-cnp-txt")!.TextContent);

        // A plain HTML form posts the ISO code.
        var hidden = document.QuerySelector("input[type=hidden]")!;
        Assert.AreEqual("country", hidden.GetAttribute("name"));
        Assert.AreEqual("JP", hidden.GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitCountryPickerShouldPrerenderTheNamesAndDescriptionsOfTheField()
    {
        var document = await PrerenderAsync(parameters =>
        {
            parameters[nameof(BitCountryPicker.Label)] = "Country";
            parameters[nameof(BitCountryPicker.Description)] = "Where the parcel goes.";
            parameters[nameof(BitCountryPicker.ErrorMessage)] = "We do not ship there.";
            parameters[nameof(BitCountryPicker.Required)] = true;
        });

        var field = document.QuerySelector("button.bit-cnp-fld")!;
        var label = document.QuerySelector(".bit-cnp-lbl")!;

        Assert.AreEqual("Country", label.TextContent.Trim());
        Assert.AreEqual(label.Id, field.GetAttribute("aria-labelledby"));

        var describedBy = field.GetAttribute("aria-describedby")!;
        StringAssert.Contains(describedBy, document.QuerySelector(".bit-cnp-erm")!.Id);
        StringAssert.Contains(describedBy, document.QuerySelector(".bit-cnp-des")!.Id);
        Assert.AreEqual("true", field.GetAttribute("aria-invalid"));
        Assert.AreEqual("true", field.GetAttribute("aria-required"));
        StringAssert.Contains(document.QuerySelector(".bit-cnp-erm")!.TextContent, "We do not ship there.");
    }

    [TestMethod]
    public async Task BitCountryPickerShouldPrerenderADisabledField()
    {
        var document = await PrerenderAsync(parameters =>
        {
            parameters[nameof(BitCountryPicker.Disabled)] = true;
        });

        var field = document.QuerySelector("button.bit-cnp-fld")!;

        Assert.IsTrue(document.QuerySelector(".bit-cnp")!.ClassList.Contains("bit-dis"));
        Assert.IsTrue(field.HasAttribute("disabled"));
        Assert.AreEqual("true", field.GetAttribute("aria-disabled"));
    }
}
