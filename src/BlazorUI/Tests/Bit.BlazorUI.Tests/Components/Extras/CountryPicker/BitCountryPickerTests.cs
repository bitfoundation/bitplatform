using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CountryPicker;

[TestClass]
public class BitCountryPickerTests : BunitTestContext
{
    private static readonly List<BitCountry> FiveCountries =
    [
        BitCountries.UnitedStates,
        BitCountries.Canada,
        BitCountries.UnitedKingdom,
        BitCountries.Germany,
        BitCountries.France,
    ];



    [TestMethod]
    public void BitCountryPickerShouldRenderExpectedElements()
    {
        var component = RenderComponent<BitCountryPicker>();

        Assert.IsNotNull(component.Find(".bit-cnp"));
        Assert.IsNotNull(component.Find("button.bit-cnp-fld"));
        Assert.IsNotNull(component.Find(".bit-cnp-cal"));
        Assert.IsNotNull(component.Find(".bit-cnp-ovl"));

        Assert.AreEqual("button", component.Find("button.bit-cnp-fld").GetAttribute("type"));
    }

    [TestMethod]
    public void BitCountryPickerShouldBeASelectOnlyCombobox()
    {
        var component = RenderComponent<BitCountryPicker>();

        var field = component.Find("button.bit-cnp-fld");

        Assert.AreEqual("combobox", field.GetAttribute("role"));
        Assert.AreEqual("listbox", field.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", field.GetAttribute("aria-expanded"));
        Assert.IsNull(field.GetAttribute("aria-controls"));

        field.Click();

        field = component.Find("button.bit-cnp-fld");
        var list = component.Find(".bit-cnp-lst");
        var search = component.Find(".bit-cnp-srch");

        Assert.AreEqual("true", field.GetAttribute("aria-expanded"));
        Assert.AreEqual("listbox", list.GetAttribute("role"));
        Assert.AreEqual(list.Id, field.GetAttribute("aria-controls"));
        Assert.AreEqual(list.Id, search.GetAttribute("aria-controls"));
        Assert.AreEqual("combobox", search.GetAttribute("role"));

        var options = component.FindAll("button.bit-cnp-itm");
        Assert.AreEqual("option", options[0].GetAttribute("role"));

        // Every option is out of the tab order: the list is driven by aria-activedescendant, not by tabbing through
        // 240 buttons.
        Assert.IsTrue(options.All(o => o.GetAttribute("tabindex") == "-1"));
        Assert.AreEqual(options[0].Id, search.GetAttribute("aria-activedescendant"));

        // With a search box the field names no option: the search box does.
        Assert.IsNull(field.GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitCountryPickerShouldNameTheActiveOptionOnTheFieldWithoutASearchBox()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-cnp-fld").Click();

        var options = component.FindAll("button.bit-cnp-itm");

        Assert.AreEqual(0, component.FindAll(".bit-cnp-srch").Count);
        Assert.AreEqual(options[0].Id, component.Find("button.bit-cnp-fld").GetAttribute("aria-activedescendant"));

        component.Find("button.bit-cnp-fld").KeyDown("ArrowDown");

        Assert.AreEqual(component.FindAll("button.bit-cnp-itm")[1].Id, component.Find("button.bit-cnp-fld").GetAttribute("aria-activedescendant"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitCountryPickerShouldRespectDisabled(bool isEnabled)
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        var root = component.Find(".bit-cnp");
        var field = component.Find("button.bit-cnp-fld");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(isEnabled is false, field.HasAttribute("disabled"));
        Assert.AreEqual(isEnabled ? null : "true", field.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectReadOnly()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        var field = component.Find("button.bit-cnp-fld");

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-cnp-rdo"));
        Assert.AreEqual("true", field.GetAttribute("aria-readonly"));

        // A ReadOnly field stays focusable, so its country is still read out, but it never opens its list.
        Assert.IsFalse(field.HasAttribute("disabled"));

        field.Click();
        field.KeyDown("ArrowDown");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldNotOpenWhenDisabled()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        component.InvokeAsync(() => component.Instance.OpenAsync()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectLabel()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Nationality");
        });

        var label = component.Find("label.bit-cnp-lbl");
        var field = component.Find("button.bit-cnp-fld");

        Assert.AreEqual("Nationality", label.TextContent.Trim());

        // A for attribute would forward the click of the label to the field and open the list, so the label names the
        // field through aria-labelledby instead.
        Assert.IsNull(label.GetAttribute("for"));
        Assert.AreEqual(label.Id, field.GetAttribute("aria-labelledby"));
        Assert.IsNull(field.GetAttribute("aria-label"));

        field.Click();

        Assert.AreEqual(label.Id, component.Find(".bit-cnp-lst").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCountryPickerShouldNotOpenOnAClickOfItsLabel()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Nationality");
        });

        component.Find("label.bit-cnp-lbl").Click();

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldBeNamedWithoutALabel()
    {
        var component = RenderComponent<BitCountryPicker>();

        var field = component.Find("button.bit-cnp-fld");

        // A combobox takes no name from its content, so a field with no label is still named.
        Assert.AreEqual("Country", field.GetAttribute("aria-label"));
        Assert.IsNull(field.GetAttribute("aria-labelledby"));

        field.Click();

        Assert.AreEqual("Countries", component.Find(".bit-cnp-lst").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCountryPickerShouldPreferItsAriaLabelOverItsLabel()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Nationality");
            parameters.Add(p => p.AriaLabel, "Staatsangehörigkeit");
        });

        var field = component.Find("button.bit-cnp-fld");

        Assert.AreEqual("Staatsangehörigkeit", field.GetAttribute("aria-label"));
        Assert.IsNull(field.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectRequired()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Label, "Nationality");
        });

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-cnp-req"));
        Assert.AreEqual("true", component.Find("button.bit-cnp-fld").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitCountryPickerShouldShowThePlaceholderWithoutASelection()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Select a country");
        });

        Assert.AreEqual("Select a country", component.Find(".bit-cnp-pls").TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-cnp-fld .bit-cnp-flg").Count);
    }

    [TestMethod]
    public void BitCountryPickerShouldShowTheFlagAndTheNameOfTheSelection()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Value, BitCountries.Germany);
        });

        Assert.AreEqual("Germany", component.Find(".bit-cnp-txt").TextContent.Trim());

        // A narrow field cuts the name short, so the field is titled with it.
        Assert.AreEqual("Germany", component.Find("button.bit-cnp-fld").GetAttribute("title"));

        var flag = component.Find(".bit-cnp-fld img.bit-cnp-flg");

        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/DE-flat-16.webp", flag.GetAttribute("src"));

        // The name is written beside it, so the flag is decoration a screen reader leaves alone.
        Assert.AreEqual("", flag.GetAttribute("alt"));
        Assert.AreEqual("true", flag.GetAttribute("aria-hidden"));
        Assert.AreEqual(0, component.FindAll(".bit-cnp-pls").Count);
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectNoFlags()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoFlags, true);
            parameters.Add(p => p.Value, BitCountries.Germany);
        });

        component.Find("button.bit-cnp-fld").Click();

        Assert.AreEqual(0, component.FindAll(".bit-cnp-flg").Count);
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectFlagUrlSelector()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Value, BitCountries.Germany);
            parameters.Add(p => p.FlagUrlSelector, c => c.Iso2 == "DE" ? "https://example.com/de.png" : null);
            parameters.Add(p => p.Countries, FiveCountries);
        });

        Assert.AreEqual("https://example.com/de.png", component.Find(".bit-cnp-fld .bit-cnp-flg").GetAttribute("src"));

        component.Find("button.bit-cnp-fld").Click();

        // A null leaves the country with the built-in flag.
        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/FR-flat-16.webp", component.FindAll(".bit-cnp-itm .bit-cnp-flg")[4].GetAttribute("src"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRenderTheCountryListOnlyAfterItIsOpened()
    {
        var component = RenderComponent<BitCountryPicker>();

        Assert.AreEqual(0, component.FindAll("button.bit-cnp-itm").Count);

        component.Find("button.bit-cnp-fld").Click();

        Assert.AreEqual(BitCountries.All.Length, component.FindAll("button.bit-cnp-itm").Count);
    }

    [TestMethod]
    public void BitCountryPickerShouldListTheNamesOfTheCountriesWithoutTheirDialCodes()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-cnp-fld").Click();

        var names = component.FindAll(".bit-cnp-inm").Select(n => n.TextContent.Trim()).ToArray();

        CollectionAssert.AreEqual(FiveCountries.Select(c => c.Name).ToArray(), names);
        Assert.IsFalse(component.Find(".bit-cnp-lst").TextContent.Contains('+'));
    }

    [TestMethod]
    public void BitCountryPickerShouldSelectAClickedCountry()
    {
        BitCountry? changed = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.OnChange, c => changed = c);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.FindAll("button.bit-cnp-itm")[3].Click();

        Assert.AreEqual("DE", changed?.Iso2);
        Assert.AreEqual("DE", component.Instance.Value?.Iso2);
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual("Germany", component.Find(".bit-cnp-txt").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectATwoWayBoundValue()
    {
        BitCountry? bound = BitCountries.France;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.Value, bound);
            parameters.Add(p => p.ValueChanged, c => bound = c);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.FindAll("button.bit-cnp-itm")[2].Click();

        Assert.AreEqual("GB", bound?.Iso2);
    }

    [TestMethod]
    public void BitCountryPickerShouldNotChangeAOneWayBoundValue()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.Value, BitCountries.France);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.FindAll("button.bit-cnp-itm")[2].Click();

        Assert.AreEqual("FR", component.Instance.Value?.Iso2);
        Assert.AreEqual("France", component.Find(".bit-cnp-txt").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldNotReportTheSelectionOfTheSelectedCountry()
    {
        var changes = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultValue, BitCountries.Canada);
            parameters.Add(p => p.OnChange, _ => changes++);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.FindAll("button.bit-cnp-itm")[1].Click();

        Assert.AreEqual(0, changes);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldMarkTheSelectedCountryInTheList()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultValue, BitCountries.Germany);
        });

        component.Find("button.bit-cnp-fld").Click();

        var selected = component.FindAll("button.bit-cnp-itm").Where(i => i.ClassList.Contains("bit-cnp-sel")).ToList();

        Assert.AreEqual(1, selected.Count);
        Assert.AreEqual("Germany", selected[0].GetAttribute("title"));
        Assert.AreEqual("true", selected[0].GetAttribute("aria-selected"));

        // The list opens on the selection, so Enter acts on it right away.
        Assert.IsTrue(selected[0].ClassList.Contains("bit-cnp-act"));
    }

    [TestMethod]
    public void BitCountryPickerShouldMatchTheSelectionByItsIsoCode()
    {
        // A page offering translated names binds an instance of its own, which is still the same country.
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.Value, new BitCountry("Deutschland", "49", "DE", "DEU"));
        });

        component.Find("button.bit-cnp-fld").Click();

        Assert.AreEqual("Germany", component.Find("button.bit-cnp-sel").GetAttribute("title"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectDefaultValue()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, BitCountries.Canada);
        });

        Assert.AreEqual("CA", component.Instance.Value?.Iso2);
        Assert.AreEqual("Canada", component.Find(".bit-cnp-txt").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldStartAnUnboundPickerOnTheDefaultCountry()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Japan);
        });

        Assert.AreEqual("JP", component.Instance.Value?.Iso2);
    }

    [TestMethod]
    public void BitCountryPickerShouldPreferTheDefaultValueOverTheDefaultCountry()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Japan);
            parameters.Add(p => p.DefaultValue, BitCountries.Canada);
        });

        Assert.AreEqual("CA", component.Instance.Value?.Iso2);
    }

    [TestMethod]
    public void BitCountryPickerShouldLeaveABoundValueAloneForTheDefaultCountry()
    {
        BitCountry? bound = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Japan);
            parameters.Add(p => p.Value, bound);
            parameters.Add(p => p.ValueChanged, c => bound = c);
        });

        // A field the user never answered stays empty for its validation to see.
        Assert.IsNull(component.Instance.Value);
        Assert.IsNull(bound);
    }

    [TestMethod]
    public void BitCountryPickerShouldPinThePreferredCountriesToTheTopOfTheList()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.PreferredCountries, new List<BitCountry> { BitCountries.Germany, BitCountries.France });
        });

        component.Find("button.bit-cnp-fld").Click();

        var items = component.FindAll("button.bit-cnp-itm");

        Assert.AreEqual("Germany", items[0].GetAttribute("title"));
        Assert.AreEqual("France", items[1].GetAttribute("title"));
        Assert.AreEqual(BitCountries.All.Length, items.Count);

        // The rule under the last of them sets them apart from the rest.
        Assert.IsFalse(items[0].ClassList.Contains("bit-cnp-ilp"));
        Assert.IsTrue(items[1].ClassList.Contains("bit-cnp-ilp"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectCustomAndExcludedCountries()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.ExcludeCountries, new List<BitCountry> { BitCountries.Canada });
        });

        component.Find("button.bit-cnp-fld").Click();

        var titles = component.FindAll("button.bit-cnp-itm").Select(i => i.GetAttribute("title")).ToArray();

        CollectionAssert.AreEqual(new[] { "United States", "United Kingdom", "Germany", "France" }, titles);
    }

    [TestMethod,
        DataRow("gb", "United Kingdom"),
        DataRow("deu", "Germany"),
        DataRow("germ", "Germany"),
        DataRow("united sta", "United States"),
        DataRow("reunion", "Reunion"),
        DataRow("aland", "Åland Islands")]
    public void BitCountryPickerShouldRankTheClosestSearchMatchFirst(string term, string expected)
    {
        var component = RenderComponent<BitCountryPicker>();

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input(term);

        var items = component.FindAll("button.bit-cnp-itm");

        Assert.IsTrue(items.Count > 0, $"No result for '{term}'.");
        Assert.AreEqual(expected, items[0].GetAttribute("title"));
    }

    [TestMethod]
    public void BitCountryPickerShouldNotSearchByDialCode()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input("44");

        // The picker shows no dialing code, so a country found through one would be a match nobody could see the
        // reason for.
        Assert.AreEqual(0, component.FindAll("button.bit-cnp-itm").Count);
    }

    [TestMethod]
    public void BitCountryPickerShouldKeepTheWholeListForATermThatAsksForNothing()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input(" - ");

        Assert.AreEqual(FiveCountries.Count, component.FindAll("button.bit-cnp-itm").Count);
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectNoResultsMessageAndTemplate()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoResultsMessage, "nothing here");
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input("zzzzzzz");

        Assert.AreEqual(0, component.FindAll("button.bit-cnp-itm").Count);
        Assert.AreEqual("nothing here", component.Find(".bit-cnp-nor").TextContent.Trim());
        StringAssert.StartsWith(component.Find(".bit-cnp-lvr").TextContent, "nothing here");

        component.Render(parameters =>
        {
            parameters.Add(p => p.NoResultsTemplate, (RenderFragment)(builder => builder.AddMarkupContent(0, "<b class=\"tpl\">none</b>")));
        });

        Assert.AreEqual("none", component.Find(".bit-cnp-nor .tpl").TextContent);
    }

    [TestMethod]
    public void BitCountryPickerShouldAnnounceTheResultOfASearch()
    {
        var component = RenderComponent<BitCountryPicker>();

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input("germany");

        StringAssert.StartsWith(component.Find(".bit-cnp-lvr").TextContent, "1 country found");

        component.Find(".bit-cnp-srch").Input("united");

        StringAssert.Contains(component.Find(".bit-cnp-lvr").TextContent, "countries found");

        component.Render(parameters =>
        {
            parameters.Add(p => p.SearchResultsAnnouncement, count => $"{count} Länder gefunden");
        });

        component.Find(".bit-cnp-srch").Input("germany");

        StringAssert.StartsWith(component.Find(".bit-cnp-lvr").TextContent, "1 Länder gefunden");
    }

    [TestMethod]
    public void BitCountryPickerShouldReportTheSearchTerm()
    {
        string? searched = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnSearch, s => searched = s);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input("france");

        Assert.AreEqual("france", searched);
    }

    [TestMethod]
    public void BitCountryPickerShouldSelectTheCountryOfTheSearchWithEnter()
    {
        BitCountry? changed = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, c => changed = c);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").Input("germany");
        component.Find(".bit-cnp-srch").KeyDown("Enter");

        Assert.AreEqual("DE", changed?.Iso2);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectSearchBoxTexts()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.SearchBoxPlaceholder, "Suchen");
            parameters.Add(p => p.SearchBoxAriaLabel, "Land suchen");
        });

        component.Find("button.bit-cnp-fld").Click();

        var search = component.Find(".bit-cnp-srch");

        Assert.AreEqual("Suchen", search.GetAttribute("placeholder"));
        Assert.AreEqual("Land suchen", search.GetAttribute("aria-label"));
        Assert.AreEqual("off", search.GetAttribute("autocomplete"));
        Assert.AreEqual("false", search.GetAttribute("spellcheck"));
    }

    [TestMethod]
    public void BitCountryPickerShouldOpenTheCalloutWithTheArrowKeys()
    {
        var opened = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
        });

        component.Find("button.bit-cnp-fld").KeyDown("ArrowDown");

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(1, opened);
    }

    [TestMethod,
        DataRow("Home", 0),
        DataRow("End", 4)]
    public void BitCountryPickerShouldOpenOnTheFirstOrLastCountryWithHomeOrEnd(string key, int expected)
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoSearchBox, true);
        });

        component.Find("button.bit-cnp-fld").KeyDown(key);

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[expected].ClassList.Contains("bit-cnp-act"));
    }

    [TestMethod]
    public void BitCountryPickerShouldStartTheSearchWithALetterTypedOnTheClosedField()
    {
        string? searched = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.OnSearch, s => searched = s);
        });

        component.Find("button.bit-cnp-fld").KeyDown("g");

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual("g", searched);
        Assert.AreEqual("g", component.Find(".bit-cnp-srch").GetAttribute("value"));
        Assert.AreEqual("Germany", component.Find("button.bit-cnp-itm").GetAttribute("title"));
    }

    [TestMethod]
    public void BitCountryPickerShouldNotOpenOnALetterTypedWithAModifier()
    {
        var component = RenderComponent<BitCountryPicker>();

        component.Find("button.bit-cnp-fld").KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldTypeAheadWithoutASearchBox()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        // Typing on the closed field opens it on the country the letters start.
        component.Find("button.bit-cnp-fld").KeyDown("g");

        Assert.IsTrue(component.Instance.IsOpen);
        StringAssert.StartsWith(component.Find("button.bit-cnp-act").GetAttribute("title"), "G");

        component.Find("button.bit-cnp-fld").KeyDown("e");
        component.Find("button.bit-cnp-fld").KeyDown("r");

        Assert.AreEqual("Germany", component.Find("button.bit-cnp-act").GetAttribute("title"));

        // Space is part of the name while a term is being typed, and Enter takes the country the term has reached.
        component.Find("button.bit-cnp-fld").KeyDown("Enter");

        Assert.AreEqual("DE", component.Instance.Value?.Iso2);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldWrapTheKeyboardNavigationOfTheList()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").KeyDown("ArrowUp");

        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[4].ClassList.Contains("bit-cnp-act"));

        component.Find(".bit-cnp-srch").KeyDown("ArrowDown");

        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[0].ClassList.Contains("bit-cnp-act"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectNoWrapNavigation()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoWrapNavigation, true);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").KeyDown("ArrowUp");

        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[0].ClassList.Contains("bit-cnp-act"));
    }

    [TestMethod]
    public void BitCountryPickerShouldNavigateWithThePageKeys()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find("button.bit-cnp-fld").KeyDown("PageDown");

        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[10].ClassList.Contains("bit-cnp-act"));

        component.Find("button.bit-cnp-fld").KeyDown("End");

        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[BitCountries.All.Length - 1].ClassList.Contains("bit-cnp-act"));

        component.Find("button.bit-cnp-fld").KeyDown("PageDown");

        // The page keys stop at the ends rather than wrapping around them.
        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[BitCountries.All.Length - 1].ClassList.Contains("bit-cnp-act"));
    }

    [TestMethod]
    public void BitCountryPickerShouldSelectWithSpaceOnlyWithoutASearchBox()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoSearchBox, true);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find("button.bit-cnp-fld").KeyDown("ArrowDown");
        component.Find("button.bit-cnp-fld").KeyDown(" ");

        Assert.AreEqual("CA", component.Instance.Value?.Iso2);
        Assert.IsFalse(component.Instance.IsOpen);

        // The focus stayed on the field the whole time, so the choice is said out loud.
        StringAssert.StartsWith(component.Find(".bit-cnp-lvr").TextContent, "Canada");
    }

    [TestMethod]
    public void BitCountryPickerShouldCloseTheCalloutWithEscapeAndAltArrowUp()
    {
        var closed = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").KeyDown("Escape");

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        component.Find("button.bit-cnp-fld").Click();
        component.Find(".bit-cnp-srch").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        // From the search box Alt+Up only closes the list.
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(2, closed);
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitCountryPickerShouldSelectWithAltArrowUpWithoutASearchBox()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoSearchBox, true);
        });

        component.Find("button.bit-cnp-fld").Click();
        component.Find("button.bit-cnp-fld").KeyDown("ArrowDown");
        component.Find("button.bit-cnp-fld").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        Assert.AreEqual("CA", component.Instance.Value?.Iso2);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldHandTheListKeysOfTheFieldToTheSearchBox()
    {
        string? searched = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.OnSearch, s => searched = s);
        });

        component.Find("button.bit-cnp-fld").Click();

        // A Shift+Tab took the focus back to the field with the list still open.
        component.Find("button.bit-cnp-fld").KeyDown("ArrowDown");

        Assert.IsTrue(component.FindAll("button.bit-cnp-itm")[1].ClassList.Contains("bit-cnp-act"));

        component.Find("button.bit-cnp-fld").KeyDown("f");

        Assert.AreEqual("f", searched);

        // Enter presses the field, which closes the list as a click on it does.
        component.Find("button.bit-cnp-fld").KeyDown("Enter");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldPutTheOpenSearchBoxIntoTheTabOrderAfterTheField()
    {
        var closed = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.Find("button.bit-cnp-fld").Click();

        var open = LastToggle();
        Assert.AreEqual(true, open.Arguments[6]);
        Assert.AreEqual(component.Find("button.bit-cnp-fld").Id, open.Arguments[^3]);

        // The Tab of the search box is the JS side's to answer, which reports it through OnTabOut.
        component.Find(".bit-cnp-srch").KeyDown("Tab");

        Assert.IsTrue(component.Instance.IsOpen);

        component.InvokeAsync(() => component.Instance._OnTabOut()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public void BitCountryPickerShouldCloseAListWithoutASearchBoxOnTab()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        component.Find("button.bit-cnp-fld").Click();

        Assert.AreEqual("", LastToggle().Arguments[^3]);

        component.Find("button.bit-cnp-fld").KeyDown("Tab");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldRelocateItsCalloutWithItsRootAsTheChain()
    {
        var component = RenderComponent<BitCountryPicker>();

        component.Find("button.bit-cnp-fld").Click();

        var toggle = LastToggle();

        // Positioned against the field, inheriting from the root.
        Assert.AreEqual(component.Find("button.bit-cnp-fld").Id, toggle.Arguments[1]);
        Assert.AreEqual(component.Find(".bit-cnp").Id, toggle.Arguments[^2]);
    }

    [TestMethod]
    public void BitCountryPickerShouldCloseWhenAnotherCalloutOpens()
    {
        var closed = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.Find("button.bit-cnp-fld").Click();

        var toggles = CountInvocations("BitBlazorUI.Callouts.toggle");

        component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        // The JS side has already hidden it.
        Assert.AreEqual(toggles, CountInvocations("BitBlazorUI.Callouts.toggle"));
    }

    [TestMethod]
    public void BitCountryPickerShouldShowTheOverlayOnlyWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitCountryPicker>();

        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:none");

        component.Find("button.bit-cnp-fld").Click();

        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:block");

        component.Find(".bit-cnp-ovl").Click();

        Assert.IsFalse(component.Instance.IsOpen);
        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectTwoWayBoundIsOpen()
    {
        var isOpen = false;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
        });

        component.Find("button.bit-cnp-fld").Click();

        Assert.IsTrue(isOpen);

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual("false", component.Find("button.bit-cnp-fld").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitCountryPickerShouldBeControlledThroughItsPublicApi()
    {
        BitCountry? changed = null;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnChange, c => changed = c);
        });

        component.InvokeAsync(() => component.Instance.OpenAsync()).GetAwaiter().GetResult();

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual("true", component.Find("button.bit-cnp-fld").GetAttribute("aria-expanded"));

        component.InvokeAsync(() => component.Instance.CloseAsync()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);

        component.InvokeAsync(() => component.Instance.SelectCountryAsync(BitCountries.Japan)).GetAwaiter().GetResult();

        Assert.AreEqual("JP", changed?.Iso2);
        Assert.AreEqual("Japan", component.Find(".bit-cnp-txt").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectTemplates()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultValue, BitCountries.Germany);
            parameters.Add(p => p.ValueTemplate, country => builder => builder.AddMarkupContent(0, $"<i class=\"vt\">{country?.Iso3}</i>"));
            parameters.Add(p => p.ItemTemplate, country => builder => builder.AddMarkupContent(0, $"<i class=\"it\">{country.Iso2}</i>"));
        });

        Assert.AreEqual("DEU", component.Find(".bit-cnp-fld .vt").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-cnp-txt").Count);

        component.Find("button.bit-cnp-fld").Click();

        CollectionAssert.AreEqual(new[] { "US", "CA", "GB", "DE", "FR" }, component.FindAll(".bit-cnp-itm .it").Select(i => i.TextContent).ToArray());
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectDescription()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Description, "Where you live.");
        });

        var description = component.Find(".bit-cnp-des");

        Assert.AreEqual("Where you live.", description.TextContent.Trim());
        Assert.AreEqual(description.Id, component.Find("button.bit-cnp-fld").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitCountryPickerShouldPointAtBothTheErrorMessageAndTheDescription()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Description, "Where you live.");
            parameters.Add(p => p.ErrorMessage, "Not served.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["aria-describedby"] = "own-hint" });
        });

        var field = component.Find("button.bit-cnp-fld");

        Assert.AreEqual($"own-hint {component.Find(".bit-cnp-erm").Id} {component.Find(".bit-cnp-des").Id}", field.GetAttribute("aria-describedby"));
        Assert.AreEqual("true", field.GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-inv"));

        // The message appeared after the field was read, so it is announced.
        StringAssert.StartsWith(component.Find(".bit-cnp-lvr").TextContent, "Not served.");
    }

    [TestMethod]
    public void BitCountryPickerShouldMarkTheFieldInvalidWhenForcedTo()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Invalid, true);
        });

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find("button.bit-cnp-fld").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitCountryPickerShouldValidateInsideAnEditForm()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);
        Expression<Func<BitCountry?>> expression = () => model.Country;

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitCountryPicker>(0);
                builder.AddAttribute(1, nameof(BitCountryPicker.Countries), FiveCountries);
                builder.AddAttribute(2, nameof(BitCountryPicker.Value), model.Country);
                builder.AddAttribute(3, nameof(BitCountryPicker.ValueChanged), EventCallback.Factory.Create<BitCountry?>(this, c => model.Country = c));
                builder.AddAttribute(4, nameof(BitCountryPicker.ValueExpression), expression);
                builder.CloseComponent();
            }));
        });

        Assert.IsNull(component.Find("button.bit-cnp-fld").GetAttribute("aria-invalid"));

        component.InvokeAsync(() => editContext.Validate());

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", component.Find("button.bit-cnp-fld").GetAttribute("aria-invalid"));
            Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-inv"));
        });

        component.Find("button.bit-cnp-fld").Click();
        component.FindAll("button.bit-cnp-itm")[3].Click();

        Assert.AreEqual("DE", model.Country?.Iso2);

        // Picking a country notifies the form of the edit, which validates the field again.
        component.WaitForAssertion(() =>
        {
            Assert.IsNull(component.Find("button.bit-cnp-fld").GetAttribute("aria-invalid"));
            Assert.IsFalse(component.Find(".bit-cnp").ClassList.Contains("bit-inv"));
        });
    }

    [TestMethod]
    public void BitCountryPickerShouldPostTheIsoCodeOfTheSelectionWhenNamed()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Name, "country");
            parameters.Add(p => p.DefaultValue, BitCountries.Netherlands);
        });

        var hidden = component.Find("input[type=hidden]");

        Assert.AreEqual("country", hidden.GetAttribute("name"));
        Assert.AreEqual("NL", hidden.GetAttribute("value"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRenderNoHiddenFieldWithoutAName()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, BitCountries.Netherlands);
        });

        Assert.AreEqual(0, component.FindAll("input[type=hidden]").Count);
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-cnp-sm"),
        DataRow(BitSize.Medium, "bit-cnp-md"),
        DataRow(BitSize.Large, "bit-cnp-lg")]
    public void BitCountryPickerShouldRespectSize(BitSize size, string cssClass)
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains(cssClass));

        // The callout is rendered outside the root, so it carries the class itself.
        Assert.IsTrue(component.Find(".bit-cnp-cal").ClassList.Contains(cssClass));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-cnp-pri"),
        DataRow(BitColor.Error, "bit-cnp-err"),
        DataRow(BitColor.TertiaryBorder, "bit-cnp-tbr")]
    public void BitCountryPickerShouldRespectColor(BitColor color, string cssClass)
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains(cssClass));
        Assert.IsTrue(component.Find(".bit-cnp-cal").ClassList.Contains(cssClass));
    }

    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-cnp-bpr", "bit-cnp-brp"),
        DataRow(BitColorKind.Secondary, "bit-cnp-bse", "bit-cnp-brs"),
        DataRow(BitColorKind.Tertiary, "bit-cnp-btr", "bit-cnp-brt"),
        DataRow(BitColorKind.Transparent, "bit-cnp-btn", "bit-cnp-brn")]
    public void BitCountryPickerShouldRespectBackgroundAndBorder(BitColorKind kind, string background, string border)
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Background, kind);
            parameters.Add(p => p.Border, kind);
        });

        var root = component.Find(".bit-cnp");

        Assert.IsTrue(root.ClassList.Contains(background));
        Assert.IsTrue(root.ClassList.Contains(border));
    }

    [TestMethod]
    public void BitCountryPickerShouldPublishNoColorOrSizeWhileTheyAreUnset()
    {
        var component = RenderComponent<BitCountryPicker>();

        // The public --bit-CountryPicker-* variables restyle the default, which a class carrying it would hide.
        var classes = component.Find(".bit-cnp").ClassList.Concat(component.Find(".bit-cnp-cal").ClassList).ToArray();

        foreach (var cls in new[] { "bit-cnp-sm", "bit-cnp-md", "bit-cnp-lg", "bit-cnp-pri", "bit-cnp-bpr", "bit-cnp-brp" })
        {
            Assert.IsFalse(classes.Contains(cls), cls);
        }
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectAppearanceParameters()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoBorder, true);
            parameters.Add(p => p.Underlined, true);
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Title, "Your country");
        });

        var root = component.Find(".bit-cnp");
        var callout = component.Find(".bit-cnp-cal");

        Assert.IsTrue(root.ClassList.Contains("bit-cnp-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-und"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-cnp-res"));
        Assert.IsTrue(callout.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("Your country", root.GetAttribute("title"));
        Assert.AreEqual("Your country", component.Find("button.bit-cnp-fld").GetAttribute("title"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectResponsive()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Label, "Country");
            parameters.Add(p => p.ResponsiveCloseButtonAriaLabel, "Schließen");
            parameters.Add(p => p.ResponsiveCloseIcon, BitIconInfo.Css("fa-solid fa-xmark"));
        });

        Assert.AreEqual("Country", component.Find(".bit-cnp-rlb").TextContent.Trim());

        var close = component.Find("button.bit-cnp-cls");

        Assert.AreEqual("Schließen", close.GetAttribute("aria-label"));
        StringAssert.Contains(close.QuerySelector("i")!.ClassName, "fa-xmark");

        component.Find("button.bit-cnp-fld").Click();
        component.Find("button.bit-cnp-cls").Click();

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectStylesAndClasses()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultValue, BitCountries.Germany);
            parameters.Add(p => p.Styles, new BitCountryPickerClassStyles { Root = "margin:1px", Field = "color:red", Item = "color:blue" });
            parameters.Add(p => p.Classes, new BitCountryPickerClassStyles { Root = "root-class", Text = "text-class", ItemName = "name-class", Callout = "callout-class" });
        });

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("root-class"));
        StringAssert.Contains(component.Find(".bit-cnp").GetAttribute("style"), "margin:1px");
        Assert.AreEqual("color:red", component.Find(".bit-cnp-fld").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-cnp-tcn").ClassList.Contains("text-class"));
        Assert.IsTrue(component.Find(".bit-cnp-cal").ClassList.Contains("callout-class"));

        component.Find("button.bit-cnp-fld").Click();

        Assert.AreEqual("color:blue", component.Find(".bit-cnp-itm").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-cnp-inm").ClassList.Contains("name-class"));
    }

    [TestMethod]
    public void BitCountryPickerShouldKeepTheFocusRingWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitCountryPicker>();

        var field = component.Find("button.bit-cnp-fld");

        field.Focus();

        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-cnp-fcs"));

        field.Click();
        component.Find("button.bit-cnp-fld").Blur();

        // The focus went into the search box of the list, which is part of the field as far as the user is concerned.
        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-cnp-fcs"));

        component.Find(".bit-cnp-srch").KeyDown("Escape");

        // Escape gives the focus back to the field.
        Assert.IsTrue(component.Find(".bit-cnp").ClassList.Contains("bit-cnp-fcs"));

        component.Find("button.bit-cnp-fld").Blur();

        Assert.IsFalse(component.Find(".bit-cnp").ClassList.Contains("bit-cnp-fcs"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRaiseTheFocusEvents()
    {
        var focused = 0;
        var blurred = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => focused++);
            parameters.Add(p => p.OnBlur, () => blurred++);
        });

        component.Find("button.bit-cnp-fld").Focus();
        component.Find("button.bit-cnp-fld").Blur();

        Assert.AreEqual(1, focused);
        Assert.AreEqual(1, blurred);
    }

    [TestMethod]
    public void BitCountryPickerShouldSplatTheInputAttributesOnTheField()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["data-test"] = "field", ["title"] = "own title" });
        });

        var field = component.Find("button.bit-cnp-fld");

        Assert.AreEqual("field", field.GetAttribute("data-test"));
        Assert.AreEqual("own title", field.GetAttribute("title"));
    }

    [TestMethod]
    public void BitCountryPickerShouldRespectTabIndex()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("3", component.Find("button.bit-cnp-fld").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitCountryPickerShouldTakeBackItsCalloutWhenDisposed()
    {
        var component = RenderComponent<BitCountryPicker>();

        component.Find("button.bit-cnp-fld").Click();

        component.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Callouts.clear"));
    }

    [TestMethod]
    public void BitCountryPickerShouldKeepASplattedAriaOwnsWhileClosed()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitCountryPicker>(0);
            builder.AddAttribute(1, "aria-owns", "own-popup");
            builder.CloseComponent();
        });

        Assert.AreEqual("own-popup", component.Find(".bit-cnp").GetAttribute("aria-owns"));

        component.Find("button.bit-cnp-fld").Click();

        Assert.AreEqual($"own-popup {component.Find(".bit-cnp-cal").Id}", component.Find(".bit-cnp").GetAttribute("aria-owns"));
    }



    [TestMethod]
    public void BitCountryPickerShouldCloseItsListWhenDisabledWhileOpen()
    {
        var isOpen = false;
        var closed = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.Find("button.bit-cnp-fld").Click();

        Assert.IsTrue(isOpen);

        // A form disabled for its submit while the list is open.
        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        Assert.IsFalse(isOpen);
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        // The JS side is told to take the relocated list off the screen, not left holding it.
        Assert.AreEqual(false, LastToggle().Arguments[6]);
        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitCountryPickerShouldHideAListAOneWayIsOpenKeepsOpenWhileDisabled()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(true, LastToggle().Arguments[6]);

        // Every render of the page hands the one-way IsOpen down again, which is what keeps it from being set back.
        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Disabled, true);
        });

        // The picker cannot set a one-way IsOpen back to false, so the list is hidden instead.
        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(false, LastToggle().Arguments[6]);
        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:none");
        Assert.AreEqual("false", component.Find("button.bit-cnp-fld").GetAttribute("aria-expanded"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Disabled, false);
        });

        Assert.AreEqual(true, LastToggle().Arguments[6]);
        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:block");
        Assert.AreEqual("true", component.Find("button.bit-cnp-fld").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void BitCountryPickerShouldNotOpenThroughIsOpenWhenItCannotBeUsed(bool disabled, bool readOnly)
    {
        var isOpen = false;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, disabled);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsFalse(isOpen);
        Assert.IsFalse(component.Instance.IsOpen);
        StringAssert.Contains(component.Find(".bit-cnp-ovl").GetAttribute("style"), "display:none");
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Callouts.toggle" && Equals(i.Arguments[6], true)));
    }

    [TestMethod]
    public void BitCountryPickerShouldSwapTheKeysOfTheFieldWhenOpenedAndClosedThroughIsOpen()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        CollectionAssert.DoesNotContain(LastFieldKeys(), "Enter");

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // Enter picks the active country of the open list, and the click it would also fire must not reopen it.
        CollectionAssert.Contains(LastFieldKeys(), "Enter");
        CollectionAssert.Contains(LastFieldKeys(), "PageDown");

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        // Enter is the native click that opens the closed field again.
        CollectionAssert.DoesNotContain(LastFieldKeys(), "Enter");
    }

    [TestMethod]
    public void BitCountryPickerShouldPutTheFieldKeysBackWhenAnotherCalloutOpens()
    {
        var component = RenderComponent<BitCountryPicker>();

        component.Find("button.bit-cnp-fld").Click();

        CollectionAssert.Contains(LastFieldKeys(), "Enter");

        component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened()).GetAwaiter().GetResult();

        CollectionAssert.DoesNotContain(LastFieldKeys(), "Enter");
    }

    [TestMethod]
    public void BitCountryPickerShouldStartOnTheCountryOfItsListThatCarriesTheDefaultCountry()
    {
        var deutschland = new BitCountry("Deutschland", "49", "DE", "DEU");

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, [deutschland, BitCountries.France]);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
        });

        Assert.AreSame(deutschland, component.Instance.Value);
        Assert.AreEqual("Deutschland", component.Find(".bit-cnp-txt").TextContent);
    }

    [TestMethod]
    public void BitCountryPickerShouldStartOnTheCountryOfItsListThatCarriesTheDefaultValue()
    {
        var deutschland = new BitCountry("Deutschland", "49", "DE", "DEU");

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, [deutschland, BitCountries.France]);
            parameters.Add(p => p.DefaultValue, BitCountries.Germany);
        });

        Assert.AreSame(deutschland, component.Instance.Value);
    }

    [TestMethod]
    public void BitCountryPickerShouldNotStartOnADefaultCountryItsListDoesNotOffer()
    {
        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.ExcludeCountries, [BitCountries.Germany]);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
        });

        Assert.IsNull(component.Instance.Value);
        Assert.IsNotNull(component.Find(".bit-cnp-pls"));
    }

    [TestMethod]
    public void BitCountryPickerShouldSelectOnlyACountryItsListOffersThroughItsPublicApi()
    {
        var deutschland = new BitCountry("Deutschland", "49", "DE", "DEU");
        var changes = 0;

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, [deutschland, BitCountries.France]);
            parameters.Add(p => p.ExcludeCountries, [BitCountries.France]);
            parameters.Add(p => p.OnChange, _ => changes++);
        });

        component.InvokeAsync(() => component.Instance.SelectCountryAsync(BitCountries.France)).GetAwaiter().GetResult();

        Assert.IsNull(component.Instance.Value);
        Assert.AreEqual(0, changes);

        component.InvokeAsync(() => component.Instance.SelectCountryAsync(BitCountries.Germany)).GetAwaiter().GetResult();

        Assert.AreSame(deutschland, component.Instance.Value);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void BitCountryPickerShouldOfferACountryNamedTwiceOnlyOnce()
    {
        var deutschland = new BitCountry("Deutschland", "49", "DE", "DEU");

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, [BitCountries.UnitedStates, deutschland, BitCountries.UnitedStates, BitCountries.Germany]);
        });

        component.Find("button.bit-cnp-fld").Click();

        var names = component.FindAll(".bit-cnp-inm").Select(n => n.TextContent).ToArray();

        // The first entry carrying a code is the one offered.
        CollectionAssert.AreEqual(new[] { "United States", "Deutschland" }, names);
    }

    [TestMethod]
    public void BitCountryPickerShouldDrawAnEmptyBoxForACountryWithNoFlag()
    {
        var neverland = new BitCountry("Neverland", "999", "ZZ", "ZZZ");

        var component = RenderComponent<BitCountryPicker>(parameters =>
        {
            parameters.Add(p => p.Countries, [neverland, BitCountries.France]);
            parameters.Add(p => p.Value, neverland);
        });

        Assert.AreEqual(0, component.FindAll(".bit-cnp-fld img").Count);
        Assert.AreEqual("SPAN", component.Find(".bit-cnp-fld .bit-cnp-flg").TagName);

        component.Find("button.bit-cnp-fld").Click();

        var rows = component.FindAll(".bit-cnp-itm");

        Assert.AreEqual("SPAN", rows[0].QuerySelector(".bit-cnp-flg")!.TagName);
        Assert.AreEqual("IMG", rows[1].QuerySelector(".bit-cnp-flg")!.TagName);
    }

    [TestMethod]
    public void BitCountryPickerShouldListenToOneFocusEventPerDirectionOnItsField()
    {
        var component = RenderComponent<BitCountryPicker>();

        var field = component.Find("button.bit-cnp-fld");

        Assert.IsTrue(field.HasAttribute("blazor:onfocus"));
        Assert.IsTrue(field.HasAttribute("blazor:onblur"));
        Assert.IsFalse(field.HasAttribute("blazor:onfocusin"));
        Assert.IsFalse(field.HasAttribute("blazor:onfocusout"));
    }



    // The keys the field's own listener was last handed: the field's sets are the ones that carry Home, which the
    // search box leaves to its caret.
    private string[] LastFieldKeys()
    {
        return (string[])Context.JSInterop.Invocations
                                .Last(i => i.Identifier == "BitBlazorUI.Extras.setPreventKeys" && ((string[])i.Arguments[1]!).Contains("Home"))
                                .Arguments[1]!;
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Rtl, true),
        DataRow(BitDir.Ltr, false),
        DataRow(BitDir.Auto, null)]
    public void BitCountryPickerShouldHandTheCalloutOnlyADirectionItsDirNames(BitDir? dir, bool? expected)
    {
        var component = RenderComponent<BitCountryPicker>(parameters => parameters.Add(p => p.Dir, dir));

        component.Find("button.bit-cnp-fld").Click();

        // A Dir left unset inherits the page's direction, and Auto takes the content's: the script reads it off the
        // element itself then (isRtl: null), rather than being told the page is left to right.
        Assert.AreEqual(expected, LastToggle().Arguments[9]);
    }

    private int CountInvocations(string identifier)
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier == identifier);
    }

    private JSRuntimeInvocation LastToggle()
    {
        return Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");
    }

    private class ValidationModel
    {
        [Required]
        public BitCountry? Country { get; set; }
    }
}
