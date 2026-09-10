using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PhoneInput;

[TestClass]
public class BitPhoneInputTests : BunitTestContext
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
    public void BitPhoneInputShouldRenderExpectedElements()
    {
        var component = RenderComponent<BitPhoneInput>();

        Assert.IsNotNull(component.Find(".bit-phi"));
        Assert.IsNotNull(component.Find(".bit-phi-fgp"));
        Assert.IsNotNull(component.Find(".bit-phi-drp"));
        Assert.IsNotNull(component.Find("input.bit-phi-inp"));
        Assert.IsNotNull(component.Find(".bit-phi-cal"));

        var input = component.Find("input.bit-phi-inp");
        Assert.AreEqual("tel", input.GetAttribute("type"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitPhoneInputShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-phi");
        var input = component.Find("input.bit-phi-inp");
        var dropdown = component.Find("button.bit-phi-drp");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(isEnabled is false, input.HasAttribute("disabled"));
        Assert.AreEqual(isEnabled is false, dropdown.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectReadOnly()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.IsTrue(component.Find("input.bit-phi-inp").HasAttribute("readonly"));
        Assert.AreEqual("-1", component.Find("button.bit-phi-drp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectLabel()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Label, "Phone number");
        });

        var label = component.Find("label.bit-phi-lbl");
        var input = component.Find("input.bit-phi-inp");

        Assert.AreEqual("Phone number", label.TextContent.Trim());
        Assert.AreEqual(input.Id, label.GetAttribute("for"));
        Assert.AreEqual(label.Id, input.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectRequired()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Label, "Phone number");
        });

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-phi-req"));
        Assert.AreEqual("true", component.Find("input.bit-phi-inp").GetAttribute("aria-required"));
        Assert.IsTrue(component.Find("input.bit-phi-inp").HasAttribute("required"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-phi-sm"),
        DataRow(BitSize.Medium, "bit-phi-md"),
        DataRow(BitSize.Large, "bit-phi-lg")]
    public void BitPhoneInputShouldRespectSize(BitSize size, string cssClass)
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains(cssClass));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-phi-pri"),
        DataRow(BitColor.Secondary, "bit-phi-sec"),
        DataRow(BitColor.Tertiary, "bit-phi-ter"),
        DataRow(BitColor.Info, "bit-phi-inf"),
        DataRow(BitColor.Success, "bit-phi-suc"),
        DataRow(BitColor.Warning, "bit-phi-wrn"),
        DataRow(BitColor.SevereWarning, "bit-phi-swr"),
        DataRow(BitColor.Error, "bit-phi-err")]
    public void BitPhoneInputShouldRespectColor(BitColor color, string cssClass)
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains(cssClass));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectAppearanceParameters()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.FullWidth, true);
            parameters.Add(p => p.NoBorder, true);
            parameters.Add(p => p.Underlined, true);
            parameters.Add(p => p.Invalid, true);
        });

        var root = component.Find(".bit-phi");

        Assert.IsTrue(root.ClassList.Contains("bit-phi-fwd"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectTitleAndMaxLength()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Title, "the tooltip");
            parameters.Add(p => p.MaxLength, 10);
        });

        Assert.AreEqual("the tooltip", component.Find(".bit-phi").GetAttribute("title"));
        Assert.AreEqual("10", component.Find("input.bit-phi-inp").GetAttribute("maxlength"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectDefaultCountry()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
        });

        Assert.AreEqual("+49", component.Find(".bit-phi-cod").TextContent.Trim());
        Assert.AreEqual(BitCountries.Germany, component.Instance.Country);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectDropdownPlaceholder()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DropdownPlaceholder, "Country");
        });

        Assert.AreEqual("Country", component.Find(".bit-phi-pls").TextContent.Trim());
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoDropdown()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoDropdown, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
        });

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-phi-nod"));
        Assert.AreEqual(0, component.FindAll("button.bit-phi-drp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-phi-cal").Count);
        Assert.AreEqual("+49", component.Find(".bit-phi-cod").TextContent.Trim());
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoFlagsAndNoDialCode()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.NoFlags, true);
            parameters.Add(p => p.NoDialCode, true);
        });

        Assert.AreEqual(0, component.FindAll("img.bit-phi-flg").Count);
        Assert.AreEqual(0, component.FindAll(".bit-phi-cod").Count);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoSearchBox()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-phi-srch").Count);
    }

    [TestMethod]
    public void BitPhoneInputShouldRenderHiddenInputForTheFullValueWhenNamed()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Name, "phone");
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.DefaultValue, "+4930123456");
        });

        var hidden = component.Find("input[type=hidden]");

        Assert.AreEqual("phone", hidden.GetAttribute("name"));
        Assert.AreEqual("+4930123456", hidden.GetAttribute("value"));
    }



    [TestMethod]
    public void BitPhoneInputShouldSplitTheValueIntoItsParts()
    {
        BitCountry? country = null;
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Value, "+4930123456");
            parameters.Add(p => p.ValueChanged, _ => { });
            parameters.Add(p => p.CountryChanged, c => country = c);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        Assert.AreEqual(BitCountries.Germany.Iso2, country?.Iso2);
        Assert.AreEqual("30123456", number);
        Assert.AreEqual("30123456", component.Find("input.bit-phi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitPhoneInputShouldResolveTheOwnerOfASharedDialCode()
    {
        BitCountry? country = null;

        RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Value, "+15551234567");
            parameters.Add(p => p.ValueChanged, _ => { });
            parameters.Add(p => p.CountryChanged, c => country = c);
        });

        // Canada comes first alphabetically, but the United States carries the higher priority for +1.
        Assert.AreEqual("US", country?.Iso2);
    }

    [TestMethod]
    public void BitPhoneInputShouldLetPreferredCountriesWinASharedDialCode()
    {
        BitCountry? country = null;

        RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Value, "+15551234567");
            parameters.Add(p => p.PreferredCountries, new List<BitCountry> { BitCountries.Canada });
            parameters.Add(p => p.ValueChanged, _ => { });
            parameters.Add(p => p.CountryChanged, c => country = c);
        });

        Assert.AreEqual("CA", country?.Iso2);
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepTheCurrentCountryOfASharedDialCode()
    {
        BitCountry? country = BitCountries.Canada;

        RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Canada);
            parameters.Add(p => p.CountryChanged, c => country = c);
            parameters.Add(p => p.Value, "+15551234567");
            parameters.Add(p => p.ValueChanged, _ => { });
        });

        Assert.AreEqual("CA", country?.Iso2);
    }

    [TestMethod]
    public void BitPhoneInputShouldReadTheInternationalCallPrefix()
    {
        BitCountry? country = null;
        string? number = null;

        RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Value, "004930123456");
            parameters.Add(p => p.ValueChanged, _ => { });
            parameters.Add(p => p.CountryChanged, c => country = c);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        Assert.AreEqual("DE", country?.Iso2);
        Assert.AreEqual("30123456", number);
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepANumberWhoseDialCodeIsNotInTheList()
    {
        string? value = "+9991234567";

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        // Nothing claims +999, so the number stays whole instead of being prefixed a second time.
        Assert.AreEqual("+9991234567", component.Instance.FullNumber);
    }

    [TestMethod]
    public void BitPhoneInputShouldComposeAnE164ValueOutOfASeparatedNumber()
    {
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Change("(415) 555-0123");

        Assert.AreEqual("+14155550123", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldSwitchTheCountryOfATypedInternationalNumber()
    {
        string? value = null;
        BitCountry? country = null;
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
            parameters.Add(p => p.CountryChanged, c => country = c);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        component.Find("input.bit-phi-inp").Change("+49 30 123456");

        Assert.AreEqual("DE", country?.Iso2);
        Assert.AreEqual("30123456", number);
        Assert.AreEqual("+4930123456", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldMapAnEmptyNumberToAnEmptyValue()
    {
        string? value = "+14155550123";

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Change(string.Empty);

        Assert.IsNull(value);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectStrict()
    {
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Strict, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Input("41a5b5");

        Assert.AreEqual("+14155", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldFormatTheNumberWithAMask()
    {
        string? value = null;
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Mask, "(###) ###-####");
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        component.Find("input.bit-phi-inp").Input("4155550123");

        Assert.AreEqual("(415) 555-0123", number);

        // The separators are display only: the value stays the plain E.164 number.
        Assert.AreEqual("+14155550123", value);
    }

    [TestMethod,
        DataRow("4", "(4"),
        DataRow("415", "(415"),
        DataRow("4155", "(415) 5"),
        DataRow("4155550123", "(415) 555-0123"),
        DataRow("41555501234567", "(415) 555-0123" + "4567")]
    public void BitPhoneInputShouldLayTheDigitsOutOverTheMaskAsTheyCome(string typed, string expected)
    {
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Mask, "(###) ###-####");
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        component.Find("input.bit-phi-inp").Input(typed);

        Assert.AreEqual(expected, number);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectMaskSelector()
    {
        string? number = null;
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.MaskSelector, c => c?.Iso2 == "US" ? "(###) ###-####" : "## ## ## ##");
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.NumberChanged, n => number = n);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Input("41555501");
        Assert.AreEqual("(415) 555-01", number);

        component.Find("button.bit-phi-drp").Click();
        component.FindAll("button.bit-phi-itm")
                 .First(i => i.GetAttribute("title") == BitCountries.France.Name)
                 .Click();

        // The digits already typed are laid out again over the pattern of the new country.
        Assert.AreEqual("41 55 55 01", number);
        Assert.AreEqual("+3341555501", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotMaskANumberNoCountryClaims()
    {
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.Mask, "(###) ###-####");
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        component.Find("input.bit-phi-inp").Change("+9991234567");

        Assert.AreEqual("+9991234567", number);
    }

    [TestMethod]
    public void BitPhoneInputShouldRecomposeTheValueOnACountryChange()
    {
        string? value = null;
        BitCountry? changed = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
            parameters.Add(p => p.OnCountryChange, c => changed = c);
        });

        component.Find("input.bit-phi-inp").Change("5550123");
        component.Find("button.bit-phi-drp").Click();

        var germany = component.FindAll("button.bit-phi-itm")
                               .First(i => i.GetAttribute("title") == BitCountries.Germany.Name);
        germany.Click();

        Assert.AreEqual("DE", changed?.Iso2);
        Assert.AreEqual("+495550123", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldClearOnlyTheNumber()
    {
        string? value = null;
        var cleared = false;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
            parameters.Add(p => p.OnClear, () => cleared = true);
        });

        Assert.AreEqual(0, component.FindAll("button.bit-phi-cbt").Count);

        component.Find("input.bit-phi-inp").Change("5550123");

        component.Find("button.bit-phi-cbt").Click();

        Assert.IsTrue(cleared);
        Assert.IsNull(value);
        Assert.AreEqual(BitCountries.UnitedStates, component.Instance.Country);
    }



    [TestMethod]
    public void BitPhoneInputShouldRenderTheCountryListOnlyAfterItIsOpened()
    {
        var component = RenderComponent<BitPhoneInput>();

        Assert.AreEqual(0, component.FindAll("button.bit-phi-itm").Count);

        component.Find("button.bit-phi-drp").Click();

        Assert.AreEqual(BitCountries.All.Length, component.FindAll("button.bit-phi-itm").Count);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectTheAriaContractOfTheCallout()
    {
        var component = RenderComponent<BitPhoneInput>();

        var dropdown = component.Find("button.bit-phi-drp");

        // The search box of the open callout is the combobox of the listbox it filters, so the button
        // that opens it stays the disclosure button it is: two comboboxes for one listbox would be two
        // answers to the same question.
        Assert.IsNull(dropdown.GetAttribute("role"));
        Assert.AreEqual("listbox", dropdown.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", dropdown.GetAttribute("aria-expanded"));

        dropdown.Click();

        dropdown = component.Find("button.bit-phi-drp");
        var list = component.Find(".bit-phi-lst");
        var search = component.Find(".bit-phi-srch");

        Assert.AreEqual("true", dropdown.GetAttribute("aria-expanded"));
        Assert.AreEqual("listbox", list.GetAttribute("role"));
        Assert.AreEqual(list.Id, dropdown.GetAttribute("aria-controls"));
        Assert.AreEqual(list.Id, search.GetAttribute("aria-controls"));
        Assert.AreEqual("combobox", search.GetAttribute("role"));

        var options = component.FindAll("button.bit-phi-itm");
        Assert.AreEqual("option", options[0].GetAttribute("role"));

        // Every option is out of the tab order: the list is driven by aria-activedescendant, not by
        // tabbing through 240 buttons.
        Assert.IsTrue(options.All(o => o.GetAttribute("tabindex") == "-1"));
        Assert.AreEqual(options[0].Id, search.GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitPhoneInputShouldMarkTheSelectedCountryInTheList()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
        });

        component.Find("button.bit-phi-drp").Click();

        var selected = component.FindAll("button.bit-phi-itm").Where(i => i.ClassList.Contains("bit-phi-sel")).ToList();

        Assert.AreEqual(1, selected.Count);
        Assert.AreEqual(BitCountries.Germany.Name, selected[0].GetAttribute("title"));
        Assert.AreEqual("true", selected[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPhoneInputShouldPinThePreferredCountriesToTheTopOfTheList()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.PreferredCountries, new List<BitCountry> { BitCountries.Germany, BitCountries.France });
        });

        component.Find("button.bit-phi-drp").Click();

        var items = component.FindAll("button.bit-phi-itm");

        Assert.AreEqual(BitCountries.Germany.Name, items[0].GetAttribute("title"));
        Assert.AreEqual(BitCountries.France.Name, items[1].GetAttribute("title"));
        Assert.AreEqual(BitCountries.All.Length, items.Count);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectCustomCountries()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-phi-drp").Click();

        Assert.AreEqual(FiveCountries.Count, component.FindAll("button.bit-phi-itm").Count);
    }

    [TestMethod,
        DataRow("+44", "United Kingdom"),
        DataRow("0044", "United Kingdom"),
        DataRow("44", "United Kingdom"),
        DataRow("gb", "United Kingdom"),
        DataRow("deu", "Germany"),
        DataRow("germ", "Germany"),
        DataRow("united sta", "United States"),
        DataRow("reunion", "Reunion")]
    public void BitPhoneInputShouldRankTheClosestSearchMatchFirst(string term, string expected)
    {
        var component = RenderComponent<BitPhoneInput>();

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input(term);

        var items = component.FindAll("button.bit-phi-itm");

        Assert.IsTrue(items.Count > 0, $"No result for '{term}'.");
        Assert.AreEqual(expected, items[0].GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldFoldTheDiacriticsOfACountryName()
    {
        var component = RenderComponent<BitPhoneInput>();

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("aland");

        var items = component.FindAll("button.bit-phi-itm");

        // Thailand also contains "aland", but only Åland Islands starts with it, so it ranks first.
        Assert.AreEqual("Åland Islands", items[0].GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoResultsMessage()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoResultsMessage, "nothing here");
        });

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("zzzzzzz");

        Assert.AreEqual(0, component.FindAll("button.bit-phi-itm").Count);
        Assert.AreEqual("nothing here", component.Find(".bit-phi-nor").TextContent.Trim());
    }

    [TestMethod]
    public void BitPhoneInputShouldReportTheSearchTerm()
    {
        string? searched = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnSearch, s => searched = s);
        });

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("france");

        Assert.AreEqual("france", searched);
    }

    [TestMethod]
    public void BitPhoneInputShouldSelectTheCountryOfTheSearchWithEnter()
    {
        BitCountry? changed = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnCountryChange, c => changed = c);
        });

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("germany");
        component.Find(".bit-phi-srch").KeyDown("Enter");

        Assert.AreEqual("DE", changed?.Iso2);
        Assert.IsFalse(component.Instance.IsOpen);
    }



    [TestMethod]
    public void BitPhoneInputShouldOpenTheCalloutWithTheArrowKeys()
    {
        var opened = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
        });

        component.Find("button.bit-phi-drp").KeyDown("ArrowDown");

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(1, opened);
    }

    [TestMethod]
    public void BitPhoneInputShouldCloseTheCalloutWithEscapeAndTab()
    {
        var closed = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").KeyDown("Escape");

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").KeyDown("Tab");

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(2, closed);
    }

    [TestMethod]
    public void BitPhoneInputShouldWrapTheKeyboardNavigationOfTheList()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        var dropdown = component.Find("button.bit-phi-drp");
        dropdown.Click();

        var search = component.Find(".bit-phi-srch");

        // The first option is active on opening, so ArrowUp wraps around to the last one.
        search.KeyDown("ArrowUp");

        var last = component.FindAll("button.bit-phi-itm")[FiveCountries.Count - 1];
        Assert.IsTrue(last.ClassList.Contains("bit-phi-act"));

        search.KeyDown("ArrowDown");
        Assert.IsTrue(component.FindAll("button.bit-phi-itm")[0].ClassList.Contains("bit-phi-act"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoWrapNavigation()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoWrapNavigation, true);
        });

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").KeyDown("ArrowUp");

        Assert.IsTrue(component.FindAll("button.bit-phi-itm")[0].ClassList.Contains("bit-phi-act"));
    }

    [TestMethod]
    public void BitPhoneInputShouldNavigateWithHomeEndAndPageKeys()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        var dropdown = component.Find("button.bit-phi-drp");
        dropdown.Click();

        dropdown.KeyDown("End");
        Assert.IsTrue(component.FindAll("button.bit-phi-itm")[^1].ClassList.Contains("bit-phi-act"));

        dropdown.KeyDown("Home");
        Assert.IsTrue(component.FindAll("button.bit-phi-itm")[0].ClassList.Contains("bit-phi-act"));

        dropdown.KeyDown("PageDown");
        Assert.IsTrue(component.FindAll("button.bit-phi-itm")[10].ClassList.Contains("bit-phi-act"));

        dropdown.KeyDown("PageUp");
        Assert.IsTrue(component.FindAll("button.bit-phi-itm")[0].ClassList.Contains("bit-phi-act"));
    }

    [TestMethod]
    public void BitPhoneInputShouldTypeAheadWhenThereIsNoSearchBox()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoSearchBox, true);
        });

        var dropdown = component.Find("button.bit-phi-drp");
        dropdown.Click();

        dropdown.KeyDown("g");

        var active = component.FindAll("button.bit-phi-itm").First(i => i.ClassList.Contains("bit-phi-act"));
        Assert.AreEqual(BitCountries.Germany.Name, active.GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldSelectWithSpaceOnlyWithoutASearchBox()
    {
        BitCountry? changed = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.NoSearchBox, true);
            parameters.Add(p => p.OnCountryChange, c => changed = c);
        });

        var dropdown = component.Find("button.bit-phi-drp");
        dropdown.Click();
        dropdown.KeyDown(" ");

        Assert.AreEqual(FiveCountries[0].Iso2, changed?.Iso2);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotOpenTheCalloutWhenDisabledOrReadOnly()
    {
        var disabled = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        disabled.Find("button.bit-phi-drp").Click();
        Assert.IsFalse(disabled.Instance.IsOpen);

        var readOnly = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        readOnly.Find("button.bit-phi-drp").Click();
        Assert.IsFalse(readOnly.Instance.IsOpen);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectTwoWayBoundIsOpen()
    {
        var isOpen = false;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
        });

        component.Find("button.bit-phi-drp").Click();
        Assert.IsTrue(isOpen);

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(0, component.FindAll("button.bit-phi-itm").Count(i => i.ClassList.Contains("bit-phi-act")));
    }

    [TestMethod]
    public void BitPhoneInputShouldOpenTheCalloutThroughItsPublicApi()
    {
        var component = RenderComponent<BitPhoneInput>();

        component.InvokeAsync(() => component.Instance.OpenAsync()).GetAwaiter().GetResult();
        Assert.IsTrue(component.Instance.IsOpen);

        component.InvokeAsync(() => component.Instance.CloseAsync()).GetAwaiter().GetResult();
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitPhoneInputShouldSelectACountryThroughItsPublicApi()
    {
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Change("5550123");
        component.InvokeAsync(() => component.Instance.SelectCountryAsync(BitCountries.France)).GetAwaiter().GetResult();

        Assert.AreEqual("FR", component.Instance.Country?.Iso2);
        Assert.AreEqual("+335550123", value);
    }



    [TestMethod]
    public void BitPhoneInputShouldRespectTemplates()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.DropdownTemplate, c => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-dropdown");
                builder.AddContent(2, c?.Iso2);
                builder.CloseElement();
            });
            parameters.Add(p => p.ItemTemplate, c => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-item");
                builder.AddContent(2, c.Iso3);
                builder.CloseElement();
            });
        });

        Assert.AreEqual("DE", component.Find(".custom-dropdown").TextContent.Trim());

        component.Find("button.bit-phi-drp").Click();

        var items = component.FindAll(".custom-item");
        Assert.AreEqual(FiveCountries.Count, items.Count);
        Assert.AreEqual(BitCountries.UnitedStates.Iso3, items[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoResultsTemplate()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoResultsTemplate, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-empty");
                builder.AddContent(2, "empty");
                builder.CloseElement();
            });
        });

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("zzzzzzz");

        Assert.AreEqual("empty", component.Find(".custom-empty").TextContent.Trim());
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectStylesAndClasses()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.Classes, new BitPhoneInputClassStyles
            {
                Root = "custom-root",
                Input = "custom-input",
                FieldGroup = "custom-field-group",
                Callout = "custom-callout",
            });
            parameters.Add(p => p.Styles, new BitPhoneInputClassStyles
            {
                Input = "color: red;",
            });
        });

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-phi-fgp").ClassList.Contains("custom-field-group"));
        Assert.IsTrue(component.Find(".bit-phi-cal").ClassList.Contains("custom-callout"));

        var input = component.Find("input.bit-phi-inp");
        Assert.IsTrue(input.ClassList.Contains("custom-input"));
        Assert.AreEqual("color: red;", input.GetAttribute("style"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectResponsive()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Label, "Phone number");
        });

        var callout = component.Find(".bit-phi-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-phi-res"));
        Assert.AreEqual("Phone number", component.Find(".bit-phi-rlb").TextContent.Trim());
        Assert.IsNotNull(component.Find("button.bit-phi-cls"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectRtl()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", component.Find(".bit-phi").GetAttribute("dir"));
        Assert.IsTrue(component.Find(".bit-phi-cal").ClassList.Contains("bit-phi-rtl"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRaiseTheInputEvents()
    {
        var keyDowns = 0;
        var enters = 0;
        var clicks = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnKeyDown, () => keyDowns++);
            parameters.Add(p => p.OnEnter, () => enters++);
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        var input = component.Find("input.bit-phi-inp");

        input.KeyDown("a");
        input.KeyDown("Enter");
        input.Click();

        Assert.AreEqual(2, keyDowns);
        Assert.AreEqual(1, enters);
        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepTheFocusRingWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitPhoneInput>();

        component.Find("button.bit-phi-drp").FocusIn();
        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-phi-fcs"));

        component.Find("button.bit-phi-drp").Click();

        // Opening moves the focus into the callout, which is still the same field to the user.
        component.Find("button.bit-phi-drp").FocusOut();
        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-phi-fcs"));

        component.Find(".bit-phi-srch").KeyDown("Escape");
        component.Find("button.bit-phi-drp").FocusOut();
        Assert.IsFalse(component.Find(".bit-phi").ClassList.Contains("bit-phi-fcs"));
    }



    [TestMethod]
    public void BitPhoneInputShouldNameTheCountrySelectorAfterItsSelection()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.NoDialCode, true);
            parameters.Add(p => p.NoFlags, true);
        });

        // An aria-label replaces the content of the button, so it has to carry the country itself.
        Assert.AreEqual("Country: Germany", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.DropdownAriaLabel, "Dialing code"));

        Assert.AreEqual("Dialing code", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPhoneInputShouldNameTheCountrySelectorWithoutASelection()
    {
        var component = RenderComponent<BitPhoneInput>();

        Assert.AreEqual("Select country", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.DropdownPlaceholder, "Country"));

        Assert.AreEqual("Country", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepTheFlagImagesOutOfTheAccessibilityTree()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
        });

        component.Find("button.bit-phi-drp").Click();

        // The name of the country is already carried by the label of the button and by the text of
        // each option, so the flags beside them are decorative.
        var flags = component.FindAll("img.bit-phi-flg");

        Assert.AreEqual(FiveCountries.Count + 1, flags.Count);
        Assert.IsTrue(flags.All(f => f.GetAttribute("alt") == string.Empty));
        Assert.IsTrue(flags.All(f => f.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRepeatTheSizeClassOnTheCallout()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        // The callout is rendered outside the root element, so it needs a class carrying the size
        // variables of its own.
        Assert.IsTrue(component.Find(".bit-phi-cal").ClassList.Contains("bit-phi-lg"));

        var medium = RenderComponent<BitPhoneInput>();
        Assert.IsTrue(medium.Find(".bit-phi-cal").ClassList.Contains("bit-phi-md"));
    }

    [TestMethod]
    public void BitPhoneInputShouldOfferTheOwnerOfASharedDialCodeFirstInTheSearch()
    {
        var component = RenderComponent<BitPhoneInput>();

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("+1");

        var items = component.FindAll("button.bit-phi-itm");

        Assert.AreEqual(BitCountries.UnitedStates.Name, items[0].GetAttribute("title"));
        Assert.AreEqual(BitCountries.Canada.Name, items[1].GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRaiseTheFocusEvents()
    {
        var focus = 0;
        var blur = 0;
        var focusIn = 0;
        var focusOut = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => focus++);
            parameters.Add(p => p.OnBlur, () => blur++);
            parameters.Add(p => p.OnFocusIn, () => focusIn++);
            parameters.Add(p => p.OnFocusOut, () => focusOut++);
        });

        var input = component.Find("input.bit-phi-inp");

        input.Focus();
        input.FocusIn();
        input.FocusOut();
        input.Blur();

        Assert.AreEqual(1, focus);
        Assert.AreEqual(1, blur);
        Assert.AreEqual(1, focusIn);
        Assert.AreEqual(1, focusOut);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotRaiseTheInputEventsWhenDisabled()
    {
        var keyDowns = 0;
        var clicks = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnKeyDown, () => keyDowns++);
            parameters.Add(p => p.OnClick, () => clicks++);
        });

        var input = component.Find("input.bit-phi-inp");

        input.KeyDown("a");
        input.Click();

        Assert.AreEqual(0, keyDowns);
        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotChangeAOneWayBoundCountry()
    {
        var changed = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.Country, BitCountries.UnitedStates);
            parameters.Add(p => p.OnCountryChange, _ => changed++);
        });

        component.Find("button.bit-phi-drp").Click();
        component.FindAll("button.bit-phi-itm")
                 .First(i => i.GetAttribute("title") == BitCountries.France.Name)
                 .Click();

        // Country is controlled without a CountryChanged callback, so the selection cannot move, and
        // reporting a change that never happened would desynchronize the consumer.
        Assert.AreEqual(0, changed);
        Assert.AreEqual("US", component.Instance.Country?.Iso2);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectATwoWayBoundNumber()
    {
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.Number, (string?)null);
            parameters.Add(p => p.NumberChanged, (string? _) => { });
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Render(parameters => parameters.Add(p => p.Number, "30123456"));

        Assert.AreEqual("+4930123456", value);
        Assert.AreEqual("+4930123456", component.Instance.FullNumber);
    }

    [TestMethod]
    public void BitPhoneInputShouldRecomposeTheValueWhenTheCountryIsPushedIn()
    {
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Country, BitCountries.Germany);
            parameters.Add(p => p.CountryChanged, (BitCountry? _) => { });
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Change("30123456");
        Assert.AreEqual("+4930123456", value);

        component.Render(parameters => parameters.Add(p => p.Country, BitCountries.France));

        Assert.AreEqual("+3330123456", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotRenderACalloutItDoesNotHave()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoDropdown, true);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOpenChanged, (bool _) => { });
        });

        Assert.AreEqual(0, component.FindAll(".bit-phi-cal").Count);
        Assert.AreEqual(0, component.FindAll(".bit-phi-ovl").Count);
    }

    [TestMethod]
    public void BitPhoneInputShouldShowTheOverlayOnlyWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitPhoneInput>();

        StringAssert.Contains(component.Find(".bit-phi-ovl").GetAttribute("style"), "display:none");

        component.Find("button.bit-phi-drp").Click();

        StringAssert.Contains(component.Find(".bit-phi-ovl").GetAttribute("style"), "display:block");

        component.Find(".bit-phi-ovl").Click();

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectResponsiveCloseIcon()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.ResponsiveCloseIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            parameters.Add(p => p.ResponsiveCloseButtonAriaLabel, "Dismiss");
        });

        var button = component.Find("button.bit-phi-cls");

        Assert.AreEqual("Dismiss", button.GetAttribute("aria-label"));
        Assert.IsTrue(button.QuerySelector("i")!.ClassList.Contains("fa-xmark"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectClearButtonIconAndTemplate()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Number, "5550123");
            parameters.Add(p => p.NumberChanged, (string? _) => { });
            parameters.Add(p => p.ClearButtonIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            parameters.Add(p => p.ClearButtonAriaLabel, "Empty it");
        });

        var button = component.Find("button.bit-phi-cbt");

        Assert.AreEqual("Empty it", button.GetAttribute("aria-label"));
        Assert.IsTrue(button.QuerySelector("i")!.ClassList.Contains("fa-xmark"));

        component.Render(parameters => parameters.Add(p => p.ClearButtonTemplate, (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "custom-clear");
            builder.CloseElement();
        })));

        Assert.IsNotNull(component.Find(".custom-clear"));
    }



    [TestMethod]
    public void BitPhoneInputShouldNameTheDropdownAComboboxWithoutASearchBox()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoSearchBox, true);
        });

        var dropdown = component.Find("button.bit-phi-drp");

        // Without a search box the button is what the list is navigated from, so it is the combobox.
        Assert.AreEqual("combobox", dropdown.GetAttribute("role"));

        dropdown.Click();

        dropdown = component.Find("button.bit-phi-drp");
        var options = component.FindAll("button.bit-phi-itm");

        Assert.AreEqual("true", dropdown.GetAttribute("aria-expanded"));
        Assert.AreEqual(options[0].Id, dropdown.GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectExcludeCountries()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.ExcludeCountries, new List<BitCountry> { BitCountries.Germany, BitCountries.France });
        });

        component.Find("button.bit-phi-drp").Click();

        var items = component.FindAll("button.bit-phi-itm");

        Assert.AreEqual(3, items.Count);
        Assert.IsFalse(items.Any(i => i.GetAttribute("title") == BitCountries.Germany.Name));
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepAnExcludedCountryOutOfTheDialCodeLookup()
    {
        BitCountry? country = null;
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ExcludeCountries, new List<BitCountry> { BitCountries.Germany });
            parameters.Add(p => p.CountryChanged, c => country = c);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Change("+4930123456");

        // Germany is no longer offered, so nothing claims +49 and the number is kept whole.
        Assert.IsNull(country);
        Assert.AreEqual("+4930123456", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldExcludeAPreferredCountryTheListNoLongerHas()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.ExcludeCountries, new List<BitCountry> { BitCountries.Germany });
            parameters.Add(p => p.PreferredCountries, new List<BitCountry> { BitCountries.Germany, BitCountries.France });
        });

        component.Find("button.bit-phi-drp").Click();

        var items = component.FindAll("button.bit-phi-itm");

        Assert.AreEqual(4, items.Count);
        Assert.AreEqual(BitCountries.France.Name, items[0].GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectAutoPlaceholder()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.AutoPlaceholder, true);
            parameters.Add(p => p.Mask, "(###) ###-####");
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
        });

        Assert.AreEqual("(###) ###-####", component.Find("input.bit-phi-inp").GetAttribute("placeholder"));

        // A placeholder of its own always wins: the pattern only fills in for a missing one.
        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoPlaceholder, true);
            parameters.Add(p => p.Mask, "(###) ###-####");
            parameters.Add(p => p.Placeholder, "Enter your number");
        });

        Assert.AreEqual("Enter your number", component.Find("input.bit-phi-inp").GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitPhoneInputShouldReadTheAutoPlaceholderOffTheMaskSelector()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.AutoPlaceholder, true);
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.CountryChanged, (BitCountry? _) => { });
            parameters.Add(p => p.MaskSelector, (BitCountry? c) => c?.Iso2 == "US" ? "(###) ###-####" : null);
        });

        Assert.AreEqual("(###) ###-####", component.Find("input.bit-phi-inp").GetAttribute("placeholder"));

        component.Find("button.bit-phi-drp").Click();
        component.FindAll("button.bit-phi-itm").First(i => i.GetAttribute("title") == BitCountries.France.Name).Click();

        // France has no pattern of its own, so it has no placeholder to offer either.
        Assert.IsNull(component.Find("input.bit-phi-inp").GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectFlagUrlSelector()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultCountry, BitCountries.Germany);
            parameters.Add(p => p.FlagUrlSelector, (BitCountry c) => c.Iso2 == "FR" ? null : $"https://flags.test/{c.Iso2}.png");
        });

        Assert.AreEqual("https://flags.test/DE.png", component.Find(".bit-phi-drp img.bit-phi-flg").GetAttribute("src"));

        component.Find("button.bit-phi-drp").Click();

        var france = component.FindAll("button.bit-phi-itm").First(i => i.GetAttribute("title") == BitCountries.France.Name);

        // A country the selector answers nothing for keeps the flag that ships with the library.
        Assert.AreEqual("_content/Bit.BlazorUI.Extras/flags/FR-flat-16.webp", france.QuerySelector("img")!.GetAttribute("src"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRenderTheHiddenCountryFieldWhenNamed()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Name, "phone");
            parameters.Add(p => p.CountryName, "phoneCountry");
            parameters.Add(p => p.DefaultCountry, BitCountries.Canada);
            parameters.Add(p => p.Value, "+16135550123");
        });

        var hidden = component.FindAll("input[type=hidden]");

        Assert.AreEqual(2, hidden.Count);
        Assert.AreEqual("phone", hidden[0].GetAttribute("name"));
        Assert.AreEqual("+16135550123", hidden[0].GetAttribute("value"));
        Assert.AreEqual("phoneCountry", hidden[1].GetAttribute("name"));
        Assert.AreEqual("CA", hidden[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitPhoneInputShouldSetTheInputAttributesOfAPhoneField()
    {
        var component = RenderComponent<BitPhoneInput>();

        var input = component.Find("input.bit-phi-inp");

        Assert.AreEqual("tel", input.GetAttribute("type"));
        // A phone field is what a browser autofills from, and none of the text assistances of a prose
        // field has anything to say about a number.
        Assert.AreEqual("tel", input.GetAttribute("autocomplete"));
        Assert.AreEqual("false", input.GetAttribute("spellcheck"));
        Assert.AreEqual("off", input.GetAttribute("autocorrect"));
        Assert.AreEqual("off", input.GetAttribute("autocapitalize"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.AutoComplete, "tel-national");
            parameters.Add(p => p.EnterKeyHint, "send");
            parameters.Add(p => p.InputMode, BitInputMode.Numeric);
        });

        input = component.Find("input.bit-phi-inp");

        Assert.AreEqual("tel-national", input.GetAttribute("autocomplete"));
        Assert.AreEqual("send", input.GetAttribute("enterkeyhint"));
        Assert.AreEqual("numeric", input.GetAttribute("inputmode"));
    }

    [TestMethod]
    public void BitPhoneInputShouldMarkTheInputInvalidWhenForcedTo()
    {
        var component = RenderComponent<BitPhoneInput>();

        Assert.IsNull(component.Find("input.bit-phi-inp").GetAttribute("aria-invalid"));

        component.Render(parameters => parameters.Add(p => p.Invalid, true));

        Assert.AreEqual("true", component.Find("input.bit-phi-inp").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepTheSeparatorsOfANationalNumber()
    {
        string? number = null;
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.NumberChanged, n => number = n);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("input.bit-phi-inp").Change("(415) 555-0123");

        // The separators belong to the field, and only the composed value is reduced to its digits.
        Assert.AreEqual("(415) 555-0123", number);
        Assert.AreEqual("+14155550123", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldApplyStrictOnTheChangeEventToo()
    {
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Strict, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        // A number pasted into the field and left there travels on the change event alone.
        component.Find("input.bit-phi-inp").Change("(415) 555-0123");

        Assert.AreEqual("4155550123", number);
    }

    [TestMethod]
    public void BitPhoneInputShouldHoldTheFormattedNumberToItsMaxLength()
    {
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Mask, "###-###-####");
            parameters.Add(p => p.MaxLength, 7);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        component.Find("input.bit-phi-inp").Input("4155550123");

        // The separators the mask inserts are written back past the maxlength attribute of the input,
        // so the cap is applied to the formatted text rather than only to what was typed.
        Assert.AreEqual("415-555", number);
    }

    [TestMethod]
    public void BitPhoneInputShouldNotReportAClearOfAnEmptyField()
    {
        var cleared = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.OnClear, () => cleared++);
        });

        component.InvokeAsync(() => component.Instance.ClearAsync()).GetAwaiter().GetResult();

        Assert.AreEqual(0, cleared);

        component.Find("input.bit-phi-inp").Change("5550123");
        component.InvokeAsync(() => component.Instance.ClearAsync()).GetAwaiter().GetResult();

        Assert.AreEqual(1, cleared);
    }

    [TestMethod]
    public void BitPhoneInputShouldSetTheNumberThroughItsPublicApi()
    {
        string? value = null;
        string? number = null;
        BitCountry? country = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.ValueChanged, v => value = v);
            parameters.Add(p => p.NumberChanged, n => number = n);
            parameters.Add(p => p.CountryChanged, c => country = c);
        });

        component.InvokeAsync(() => component.Instance.SetNumberAsync("5550123")).GetAwaiter().GetResult();

        Assert.AreEqual("5550123", number);
        Assert.AreEqual("+15550123", value);

        // A number carrying an international prefix moves the selection, exactly as typing it does.
        component.InvokeAsync(() => component.Instance.SetNumberAsync("+81 3 1234 5678")).GetAwaiter().GetResult();

        Assert.AreEqual("JP", country?.Iso2);
        Assert.AreEqual("312345678", number);
        Assert.AreEqual("+81312345678", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldAnnounceTheResultOfASearch()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-phi-drp").Click();

        var live = component.Find(".bit-phi-lvr");

        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.AreEqual(string.Empty, live.TextContent);

        component.Find("input.bit-phi-srch").Input("united");

        Assert.IsTrue(component.Find(".bit-phi-lvr").TextContent.StartsWith("2 countries found"));

        component.Find("input.bit-phi-srch").Input("zzz");

        Assert.IsTrue(component.Find(".bit-phi-lvr").TextContent.StartsWith("No results found"));
    }

    [TestMethod]
    public void BitPhoneInputShouldAnnounceTheCountryItSwitchedTo()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.CountryChanged, (BitCountry? _) => { });
        });

        component.Find("button.bit-phi-drp").Click();
        component.FindAll("button.bit-phi-itm").First(i => i.GetAttribute("title") == BitCountries.Germany.Name).Click();

        // The callout carrying the choice is gone by the time it is made, so it is said out loud.
        Assert.IsTrue(component.Find(".bit-phi-lvr").TextContent.StartsWith("Germany, +49"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectNoFocusOnSelect()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.NoFocusOnSelect, true);
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.CountryChanged, (BitCountry? _) => { });
        });

        component.Find("button.bit-phi-drp").Click();

        var focusCalls = Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus"));

        component.FindAll("button.bit-phi-itm").First(i => i.GetAttribute("title") == BitCountries.Germany.Name).Click();

        Assert.AreEqual("DE", component.Instance.Country?.Iso2);
        Assert.AreEqual(focusCalls, Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus")));
    }



    [TestMethod]
    public void BitPhoneInputShouldNameTheNumberInputAfterItsAriaLabel()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Label, "Phone number");
        });

        var input = component.Find("input.bit-phi-inp");

        Assert.AreEqual(component.Find("label.bit-phi-lbl").Id, input.GetAttribute("aria-labelledby"));
        Assert.IsNull(input.GetAttribute("aria-label"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Label, "Phone number");
            parameters.Add(p => p.AriaLabel, "Mobile number");
        });

        input = component.Find("input.bit-phi-inp");

        // aria-labelledby wins over aria-label, so a name of its own would be thrown away by pointing
        // at the visible label as well.
        Assert.AreEqual("Mobile number", input.GetAttribute("aria-label"));
        Assert.IsNull(input.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitPhoneInputShouldAnnounceItsErrorMessage()
    {
        var component = RenderComponent<BitPhoneInput>();

        Assert.AreEqual(string.Empty, component.Find(".bit-phi-lvr").TextContent);

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "That number is already registered"));

        Assert.IsTrue(component.Find(".bit-phi-lvr").TextContent.StartsWith("That number is already registered"));
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepTheFocusRingWhileTheClearButtonIsFocused()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Number, "5550123");
            parameters.Add(p => p.NumberChanged, (string? _) => { });
        });

        component.Find("input.bit-phi-inp").FocusIn();

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-phi-fcs"));

        // Moving from the number to the button that empties it is still a focus inside the field, so
        // the ring that says where the focus is must not blink off on the way.
        component.Find("input.bit-phi-inp").FocusOut();
        component.Find("button.bit-phi-cbt").FocusIn();

        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-phi-fcs"));

        component.Find("button.bit-phi-cbt").FocusOut();

        Assert.IsFalse(component.Find(".bit-phi").ClassList.Contains("bit-phi-fcs"));
    }


    [TestMethod]
    public void BitPhoneInputShouldKeepTheWholeListForATermThatAsksForNothing()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
        });

        component.Find("button.bit-phi-drp").Click();

        // The '+' of a dialing code about to be typed asks for nothing yet, so answering it with "no
        // results" would say the country being typed does not exist.
        component.Find(".bit-phi-srch").Input("+");

        Assert.AreEqual(FiveCountries.Count, component.FindAll("button.bit-phi-itm").Count);

        component.Find(".bit-phi-srch").Input("+44");

        Assert.AreEqual(BitCountries.UnitedKingdom.Name, component.FindAll("button.bit-phi-itm")[0].GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRaiseOnEscape()
    {
        var escaped = 0;
        var keyDowns = 0;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.OnEscape, () => escaped++);
            parameters.Add(p => p.OnKeyDown, () => keyDowns++);
        });

        var input = component.Find("input.bit-phi-inp");

        input.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escaped);
        Assert.AreEqual(1, keyDowns);

        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, escaped);
        Assert.AreEqual(2, keyDowns);
    }


    [TestMethod]
    public void BitPhoneInputShouldRespectDescription()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Description, "We will text you a code");
        });

        var description = component.Find(".bit-phi-des");
        var input = component.Find("input.bit-phi-inp");

        Assert.AreEqual("We will text you a code", description.TextContent.Trim());
        Assert.AreEqual(description.Id, input.GetAttribute("aria-describedby"));

        component.Render(parameters => parameters.Add(p => p.DescriptionTemplate, (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "custom-description");
            builder.CloseElement();
        })));

        Assert.IsNotNull(component.Find(".custom-description"));
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectErrorMessage()
    {
        var component = RenderComponent<BitPhoneInput>();

        Assert.AreEqual(0, component.FindAll(".bit-phi-erm").Count);
        Assert.IsFalse(component.Find(".bit-phi").ClassList.Contains("bit-inv"));

        component.Render(parameters => parameters.Add(p => p.ErrorMessage, "That number is already registered"));

        var error = component.Find(".bit-phi-erm");
        var input = component.Find("input.bit-phi-inp");

        Assert.AreEqual("That number is already registered", error.TextContent.Trim());

        // A message saying what is wrong with the value is an invalid state as much as the flag is.
        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual(error.Id, input.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitPhoneInputShouldPointAtBothTheErrorMessageAndTheDescription()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "Too short");
            parameters.Add(p => p.Description, "Nine digits");
        });

        var describedBy = component.Find("input.bit-phi-inp").GetAttribute("aria-describedby");

        Assert.AreEqual($"{component.Find(".bit-phi-erm").Id} {component.Find(".bit-phi-des").Id}", describedBy);
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepTheDescribedByOfItsConsumer()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Description, "Nine digits");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "aria-describedby", "outside" } });
        });

        var describedBy = component.Find("input.bit-phi-inp").GetAttribute("aria-describedby");

        Assert.AreEqual($"outside {component.Find(".bit-phi-des").Id}", describedBy);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectErrorMessageTemplate()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-error");
                builder.CloseElement();
            }));
        });

        Assert.IsNotNull(component.Find(".custom-error"));
        Assert.IsTrue(component.Find(".bit-phi").ClassList.Contains("bit-inv"));
    }


    [TestMethod]
    public void BitPhoneInputShouldResolveACountryByAnExtraDialCode()
    {
        BitCountry? country = null;
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.CountryChanged, c => country = c);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        // +1-829 is the Dominican Republic, not the +1 of the United States it starts with.
        component.Find("input.bit-phi-inp").Change("+18295551234");

        Assert.AreEqual("DO", country?.Iso2);
        Assert.AreEqual("5551234", number);
    }

    [TestMethod]
    public void BitPhoneInputShouldFindACountryByAnExtraDialCode()
    {
        var component = RenderComponent<BitPhoneInput>();

        component.Find("button.bit-phi-drp").Click();
        component.Find(".bit-phi-srch").Input("1849");

        Assert.AreEqual(BitCountries.DominicanRepublic.Name, component.FindAll("button.bit-phi-itm")[0].GetAttribute("title"));
    }

    [TestMethod]
    public void BitPhoneInputShouldAdoptANumberNoCountryClaimedWhenOneIsPicked()
    {
        string? value = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Countries, FiveCountries);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.CountryChanged, (BitCountry? _) => { });
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        // Nothing in the list claims +999, so the number is kept whole with its prefix.
        component.Find("input.bit-phi-inp").Change("+9991234567");

        Assert.AreEqual("+9991234567", value);

        component.Find("button.bit-phi-drp").Click();
        component.FindAll("button.bit-phi-itm").First(i => i.GetAttribute("title") == BitCountries.Germany.Name).Click();

        // Picking a country by hand is what decides the code from then on: a value naming +999 while
        // the field shows Germany would be two answers to the same question.
        Assert.AreEqual("+499991234567", value);
        Assert.AreEqual("9991234567", component.Instance.Number);
    }

    [TestMethod]
    public void BitPhoneInputShouldRespectTabIndex()
    {
        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Number, "5550123");
            parameters.Add(p => p.NumberChanged, (string? _) => { });
        });

        Assert.AreEqual("3", component.Find("input.bit-phi-inp").GetAttribute("tabindex"));
        Assert.AreEqual("3", component.Find("button.bit-phi-drp").GetAttribute("tabindex"));
        Assert.AreEqual("3", component.Find("button.bit-phi-cbt").GetAttribute("tabindex"));

        // A field nobody can type into is out of the tab order whatever the parameter says.
        component.Render(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.AreEqual("-1", component.Find("button.bit-phi-drp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitPhoneInputShouldApplyStrictToTheNumberItIsGiven()
    {
        string? number = null;

        var component = RenderComponent<BitPhoneInput>(parameters =>
        {
            parameters.Add(p => p.Strict, true);
            parameters.Add(p => p.DefaultCountry, BitCountries.UnitedStates);
            parameters.Add(p => p.NumberChanged, n => number = n);
        });

        component.InvokeAsync(() => component.Instance.SetNumberAsync("(415) 555-0123")).GetAwaiter().GetResult();

        Assert.AreEqual("4155550123", number);
    }

    [TestMethod]
    public void BitCountryShouldExposeEveryDialCodeItAnswersTo()
    {
        CollectionAssert.AreEqual(new[] { "1809", "1829", "1849" }, BitCountries.DominicanRepublic.DigitsCodes);
        CollectionAssert.AreEqual(new[] { "1787", "1939" }, BitCountries.PuertoRico.DigitsCodes);

        // A country with a single code answers with that one alone.
        CollectionAssert.AreEqual(new[] { "49" }, BitCountries.Germany.DigitsCodes);
    }


    [TestMethod]
    public void BitCountryShouldExposeItsDigitsCode()
    {
        Assert.AreEqual("1684", BitCountries.AmericanSamoa.DigitsCode);
        Assert.AreEqual("49", BitCountries.Germany.DigitsCode);
    }

    [TestMethod]
    public void BitCountriesShouldGiveTheOwnerOfASharedDialCodeThePriority()
    {
        Assert.AreEqual(1, BitCountries.UnitedStates.Priority);
        Assert.AreEqual(0, BitCountries.Canada.Priority);
        Assert.AreEqual(1, BitCountries.Russia.Priority);
        Assert.AreEqual(0, BitCountries.Kazakhstan.Priority);
    }
}
