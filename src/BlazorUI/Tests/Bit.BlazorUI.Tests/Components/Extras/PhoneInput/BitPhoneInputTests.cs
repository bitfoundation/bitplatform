using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
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

        Assert.AreEqual("combobox", dropdown.GetAttribute("role"));
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

        component.InvokeAsync(() => component.Instance.OpenAsync());
        Assert.IsTrue(component.Instance.IsOpen);

        component.InvokeAsync(() => component.Instance.CloseAsync());
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
        component.InvokeAsync(() => component.Instance.SelectCountryAsync(BitCountries.France));

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
