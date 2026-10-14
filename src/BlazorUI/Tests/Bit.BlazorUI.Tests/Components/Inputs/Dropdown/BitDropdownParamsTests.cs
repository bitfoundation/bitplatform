using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Dropdown;

/// <summary>
/// Covers the shape of the BitParams cascade of the Dropdown: that a BitDropdownParams carries every parameter that
/// is not per dropdown and nothing the dropdown does not have, that the parameters the main tests leave out - the
/// icons, the texts, the search settings, the functions - reach the instance, that one params object reaches the
/// Options API as well as the Items one, and what an empty or a dropped params object leaves behind. How a cascaded
/// value is matched to the type arguments of a dropdown is pinned in BitDropdownTests.
/// </summary>
[TestClass]
public class BitDropdownParamsTests : BunitTestContext
{
    private static readonly Type _dropdownType = typeof(BitDropdown<BitDropdownItem<string>, string>);
    private static readonly Type _paramsType = typeof(BitDropdownParams<BitDropdownItem<string>, string>);

    // What belongs to one dropdown alone - its selection, its validation state, its open state, its templates and its
    // callbacks - is written per dropdown, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitDropdown<BitDropdownItem<string>, string>.CascadingParameters),
        nameof(BitDropdown<BitDropdownItem<string>, string>.CalloutFooterTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.CalloutHeaderTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.CaretDownTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ChildContent),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ChipTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.DefaultValues),
        nameof(BitDropdown<BitDropdownItem<string>, string>.DescriptionTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.DynamicItemTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.EmptyTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ErrorMessage),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ErrorMessageTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.HeaderTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.InitialSelectedItems),
        nameof(BitDropdown<BitDropdownItem<string>, string>.Invalid),
        nameof(BitDropdown<BitDropdownItem<string>, string>.IsOpen),
        nameof(BitDropdown<BitDropdownItem<string>, string>.IsOpenChanged),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ItemTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.LabelTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.LoadingTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.NoResultsTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnClear),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnClick),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnClose),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnDeselectItem),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnDynamicAdd),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnFocusIn),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnFocusOut),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnOpen),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnSearch),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnSelectItem),
        nameof(BitDropdown<BitDropdownItem<string>, string>.OnValuesChange),
        nameof(BitDropdown<BitDropdownItem<string>, string>.Options),
        nameof(BitDropdown<BitDropdownItem<string>, string>.PlaceholderTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.PrefixTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.SuffixTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.TextTemplate),
        nameof(BitDropdown<BitDropdownItem<string>, string>.Values),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ValuesChanged),
        nameof(BitDropdown<BitDropdownItem<string>, string>.ValuesExpression),
        nameof(BitDropdown<BitDropdownItem<string>, string>.VirtualizePlaceholder),
    ];

    private static List<BitDropdownItem<string>> GetItems() =>
    [
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora" },
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitDropdownParams<BitDropdownItem<string>, string> dropdownParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { dropdownParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropdown<BitDropdownItem<string>, string>>(0);
                builder.AddAttribute(1, nameof(BitDropdown<BitDropdownItem<string>, string>.Items), GetItems());
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitDropdownParamsShouldCarryEveryParameterThatIsNotPerDropdown()
    {
        // ReadOnly and Required are not carried either: the params object derives from BitComponentBaseParams rather
        // than from the generic BitInputBaseParams, so they count as base parameters here.
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name)
                                                     .Concat(typeof(BitInputBase<string>).GetProperties().Select(p => p.Name))
                                                     .ToHashSet();

        var parameters = _dropdownType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                      .Select(p => p.Name)
                                      .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(_paramsType.GetProperty(name), $"BitDropdownParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitDropdownParamsShouldNameNothingTheDropdownDoesNotHave()
    {
        var dropdownParameters = _dropdownType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .ToHashSet();

        var paramsProperties = _paramsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.CanWrite)
                                          .Select(p => p.Name);

        foreach (var name in paramsProperties)
        {
            Assert.IsTrue(dropdownParameters.Contains(name), $"BitDropdownParams.{name} is no parameter of the dropdown.");
        }
    }

    [TestMethod]
    public void BitDropdownParamsUpdateParametersShouldSetTheRemainingProperties()
    {
        static string generate(BitDropdownItem<string>? item) => item?.Text ?? string.Empty;
        static bool exists(ICollection<BitDropdownItem<string>> items, string text) => false;
        static BitDropdownItem<string>? find(ICollection<BitDropdownItem<string>> items, string text) => null;
        static ICollection<BitDropdownItem<string>> search(ICollection<BitDropdownItem<string>> items, string text) => items;
        static ValueTask<BitDropdownItemsProviderResult<BitDropdownItem<string>>> provide(BitDropdownItemsProviderRequest<BitDropdownItem<string>> request)
            => ValueTask.FromResult(BitDropdownItemsProviderResult.From<BitDropdownItem<string>>([], 0));

        var nameSelectors = new BitDropdownNameSelectors<BitDropdownItem<string>, string>();
        var separators = new[] { ',', ';' };

        var @params = new BitDropdownParams<BitDropdownItem<string>, string>
        {
            AutoClearSearch = true,
            AutoFocusSearchBox = true,
            AutoSelectFirstMatch = true,
            CaretDownIcon = BitIconInfo.Bi("caret-down"),
            ChipsRemoveButtonAriaLabel = "Weg mit {0}",
            ChipsRemoveIcon = BitIconInfo.Bi("x"),
            ChipsRemoveIconName = "Cancel",
            ClearButtonAriaLabel = "Alles weg",
            ClearButtonIcon = BitIconInfo.Bi("x-circle"),
            ClearButtonIconName = "Clear",
            ClearOnEscape = true,
            ComboBoxAddButtonAriaLabel = "Dazu",
            ComboBoxAddButtonIcon = BitIconInfo.Bi("plus"),
            ComboBoxAddButtonIconName = "Add",
            Dynamic = true,
            DynamicItemTextFormat = "Neu: {0}",
            DynamicValueGenerator = generate,
            EmptyText = "Leer",
            ExistsSelectedItemFunction = exists,
            FindItemFunction = find,
            HideSelectedItems = true,
            HighlightSearch = true,
            Immediate = true,
            ItemCheckIcon = BitIconInfo.Bi("check"),
            ItemCheckIconName = "CheckMark",
            ItemsProvider = provide,
            ItemsProviderDebounceTime = 250,
            LoadingText = "Lade",
            MaxDisplayedItems = 2,
            MaxSelectedItems = 3,
            MaxSelectedItemsText = "Höchstens {0}",
            MinSearchLength = 2,
            MinSearchLengthText = "Noch {0}",
            NameSelectors = nameSelectors,
            NoResultsText = "Nichts",
            NoWrapNavigation = true,
            OpenOnFocus = true,
            OverflowTextFormat = "+{0} mehr",
            OverscanCount = 7,
            PreserveCalloutWidth = true,
            Reselectable = true,
            Responsive = true,
            ResponsiveCloseButtonAriaLabel = "Zu",
            ResponsiveCloseIcon = BitIconInfo.Bi("x-lg"),
            ResponsiveCloseIconName = "ChromeClose",
            SearchBoxAriaLabel = "Suche",
            SearchBoxClearButtonAriaLabel = "Leeren",
            SearchBoxClearIcon = BitIconInfo.Bi("eraser"),
            SearchBoxClearIconName = "Delete",
            SearchBoxIcon = BitIconInfo.Bi("search"),
            SearchBoxIconName = "Zoom",
            SearchFunction = search,
            SearchIgnoreDiacritics = true,
            SearchResultsText = "{0} Treffer",
            SelectAllText = "Alle",
            SelectedItemsTextFormat = "{0} gewählt",
            SelectTextOnFocus = true,
            ShowSelectAll = true,
            StickyHeaders = true,
            ThrottleTime = 50,
            TokenSeparators = separators,
            ValueComparer = StringComparer.OrdinalIgnoreCase,
            Dir = BitDir.Rtl,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
            Visibility = BitVisibility.Hidden,
            HtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes" },
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropdown<BitDropdownItem<string>, string>>(0);
                builder.CloseComponent();
            });
        });

        var instance = component.FindComponent<BitDropdown<BitDropdownItem<string>, string>>().Instance;

        Assert.IsTrue(instance.AutoClearSearch);
        Assert.IsTrue(instance.AutoFocusSearchBox);
        Assert.IsTrue(instance.AutoSelectFirstMatch);
        Assert.AreEqual(@params.CaretDownIcon, instance.CaretDownIcon);
        Assert.AreEqual("Weg mit {0}", instance.ChipsRemoveButtonAriaLabel);
        Assert.AreEqual(@params.ChipsRemoveIcon, instance.ChipsRemoveIcon);
        Assert.AreEqual("Cancel", instance.ChipsRemoveIconName);
        Assert.AreEqual("Alles weg", instance.ClearButtonAriaLabel);
        Assert.AreEqual(@params.ClearButtonIcon, instance.ClearButtonIcon);
        Assert.AreEqual("Clear", instance.ClearButtonIconName);
        Assert.IsTrue(instance.ClearOnEscape);
        Assert.AreEqual("Dazu", instance.ComboBoxAddButtonAriaLabel);
        Assert.AreEqual(@params.ComboBoxAddButtonIcon, instance.ComboBoxAddButtonIcon);
        Assert.AreEqual("Add", instance.ComboBoxAddButtonIconName);
        Assert.IsTrue(instance.Dynamic);
        Assert.AreEqual("Neu: {0}", instance.DynamicItemTextFormat);
        Assert.AreEqual((Func<BitDropdownItem<string>?, string>)generate, instance.DynamicValueGenerator);
        Assert.AreEqual("Leer", instance.EmptyText);
        Assert.AreEqual((Func<ICollection<BitDropdownItem<string>>, string, bool>)exists, instance.ExistsSelectedItemFunction);
        Assert.AreEqual((Func<ICollection<BitDropdownItem<string>>, string, BitDropdownItem<string>?>)find, instance.FindItemFunction);
        Assert.IsTrue(instance.HideSelectedItems);
        Assert.IsTrue(instance.HighlightSearch);
        Assert.IsTrue(instance.Immediate);
        Assert.AreEqual(@params.ItemCheckIcon, instance.ItemCheckIcon);
        Assert.AreEqual("CheckMark", instance.ItemCheckIconName);
        Assert.AreEqual((BitDropdownItemsProvider<BitDropdownItem<string>>)provide, instance.ItemsProvider);
        Assert.AreEqual(250, instance.ItemsProviderDebounceTime);
        Assert.AreEqual("Lade", instance.LoadingText);
        Assert.AreEqual(2, instance.MaxDisplayedItems);
        Assert.AreEqual(3, instance.MaxSelectedItems);
        Assert.AreEqual("Höchstens {0}", instance.MaxSelectedItemsText);
        Assert.AreEqual(2, instance.MinSearchLength);
        Assert.AreEqual("Noch {0}", instance.MinSearchLengthText);
        Assert.AreSame(nameSelectors, instance.NameSelectors);
        Assert.AreEqual("Nichts", instance.NoResultsText);
        Assert.IsTrue(instance.NoWrapNavigation);
        Assert.IsTrue(instance.OpenOnFocus);
        Assert.AreEqual("+{0} mehr", instance.OverflowTextFormat);
        Assert.AreEqual(7, instance.OverscanCount);
        Assert.IsTrue(instance.PreserveCalloutWidth);
        Assert.IsTrue(instance.Reselectable);
        Assert.IsTrue(instance.Responsive);
        Assert.AreEqual("Zu", instance.ResponsiveCloseButtonAriaLabel);
        Assert.AreEqual(@params.ResponsiveCloseIcon, instance.ResponsiveCloseIcon);
        Assert.AreEqual("ChromeClose", instance.ResponsiveCloseIconName);
        Assert.AreEqual("Suche", instance.SearchBoxAriaLabel);
        Assert.AreEqual("Leeren", instance.SearchBoxClearButtonAriaLabel);
        Assert.AreEqual(@params.SearchBoxClearIcon, instance.SearchBoxClearIcon);
        Assert.AreEqual("Delete", instance.SearchBoxClearIconName);
        Assert.AreEqual(@params.SearchBoxIcon, instance.SearchBoxIcon);
        Assert.AreEqual("Zoom", instance.SearchBoxIconName);
        Assert.AreEqual((Func<ICollection<BitDropdownItem<string>>, string, ICollection<BitDropdownItem<string>>>)search, instance.SearchFunction);
        Assert.IsTrue(instance.SearchIgnoreDiacritics);
        Assert.AreEqual("{0} Treffer", instance.SearchResultsText);
        Assert.AreEqual("Alle", instance.SelectAllText);
        Assert.AreEqual("{0} gewählt", instance.SelectedItemsTextFormat);
        Assert.IsTrue(instance.SelectTextOnFocus);
        Assert.IsTrue(instance.ShowSelectAll);
        Assert.IsTrue(instance.StickyHeaders);
        Assert.AreEqual(50, instance.ThrottleTime);
        Assert.AreSame(separators, instance.TokenSeparators);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, instance.ValueComparer);
        Assert.AreEqual(BitDir.Rtl, instance.Dir);
        Assert.AreEqual("cascaded", instance.Class);
        Assert.AreEqual("margin: 1px;", instance.Style);
        Assert.AreEqual("cascaded-id", instance.Id);
        Assert.AreEqual(BitVisibility.Hidden, instance.Visibility);

        var root = component.Find(".bit-drp");
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
    }

    [TestMethod]
    public void BitDropdownParamsUpdateParametersShouldIgnoreANullDropdown()
    {
        var @params = new BitDropdownParams<BitDropdownItem<string>, string> { Color = BitColor.Success };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitDropdownParamsShouldReachTheOptionsApiToo()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitDropdownParams<BitDropdownOption<string>, string> { Color = BitColor.Info, Size = BitSize.Large, Placeholder = "Cascaded placeholder" }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropdown<BitDropdownOption<string>, string>>(0);
                builder.AddAttribute(1, nameof(BitDropdown<BitDropdownOption<string>, string>.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitDropdownOption<string>>(0);
                    b.AddAttribute(1, nameof(BitDropdownOption<string>.Text), "Apple");
                    b.AddAttribute(2, nameof(BitDropdownOption<string>.Value), "f-app");
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-drp");

        Assert.IsTrue(root.ClassList.Contains("bit-drp-inf"));
        Assert.IsTrue(root.ClassList.Contains("bit-drp-lg"));
        Assert.AreEqual("Cascaded placeholder", component.Find(".bit-drp-tdp").TextContent.Trim());
        Assert.AreEqual(1, component.FindAll("[role=option]").Count);
    }

    [TestMethod]
    public void BitDropdownShouldApplyCascadedTextsToTheMarkup()
    {
        var component = RenderWithParams(new BitDropdownParams<BitDropdownItem<string>, string>
        {
            Label = "Cascaded label",
            Prefix = "Pre",
            Suffix = "Suf",
            Description = "Cascaded description",
            AriaDescription = "Cascaded aria description",
            ShowSearchBox = true,
            SearchBoxPlaceholder = "Cascaded search",
            SearchBoxAriaLabel = "Suche",
            Chips = true,
            MultiSelect = true,
            ShowClearButton = true,
            ClearButtonAriaLabel = "Alles weg",
            ChipsRemoveButtonAriaLabel = "Weg mit {0}",
        }, builder => builder.AddAttribute(2, nameof(BitDropdown<BitDropdownItem<string>, string>.DefaultValues), new[] { "f-app" }));

        Assert.AreEqual("Cascaded label", component.Find(".bit-drp-lbl").TextContent.Trim());
        Assert.AreEqual("Pre", component.Find(".bit-drp-pre").TextContent.Trim());
        Assert.AreEqual("Suf", component.Find(".bit-drp-suf").TextContent.Trim());
        Assert.AreEqual("Cascaded description", component.Find(".bit-drp-des").TextContent.Trim());
        Assert.AreEqual("Cascaded aria description", component.Find(".bit-drp-vhd").TextContent.Trim());
        Assert.AreEqual("Cascaded search", component.Find(".bit-drp-sin").GetAttribute("placeholder"));
        Assert.AreEqual("Suche", component.Find(".bit-drp-sin").GetAttribute("aria-label"));
        Assert.AreEqual("Alles weg", component.Find(".bit-drp-clr").GetAttribute("aria-label"));
        Assert.AreEqual("Weg mit Apple", component.Find(".bit-drp-crb").GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-drp").ClassList.Contains("bit-drp-sch"));
    }

    [TestMethod]
    public void BitDropdownShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitDropdownParams<BitDropdownItem<string>, string>());

        var root = component.Find(".bit-drp");

        // Nothing cascades, so Color and Size stay unset and publish nothing, and no flag is turned on.
        foreach (var cssClass in new[] { "bit-drp-pri", "bit-drp-md", "bit-drp-und", "bit-drp-nbd", "bit-drp-trn", "bit-drp-sch", "bit-drp-req" })
        {
            Assert.IsFalse(root.ClassList.Contains(cssClass), $"An empty params object published {cssClass}.");
        }

        Assert.AreEqual(0, component.FindAll(".bit-drp-sin").Count);
        Assert.AreEqual(0, component.FindAll(".bit-drp-lbl").Count);
    }

    [TestMethod]
    public void TheHtmlAttributesOfTheInstanceShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitDropdownParams<BitDropdownItem<string>, string>
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-both"] = "from-cascade", ["data-cascaded"] = "yes" },
        }, builder =>
        {
            builder.AddAttribute(2, "data-both", "from-instance");
        });

        var root = component.Find(".bit-drp");

        Assert.AreEqual("from-instance", root.GetAttribute("data-both"));
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void TheValuesOfADroppedParamsObjectShouldGo()
    {
        var component = RenderWithParams(new BitDropdownParams<BitDropdownItem<string>, string> { Underlined = true, Label = "Cascaded label", ShowSearchBox = true });

        Assert.IsTrue(component.Find(".bit-drp").ClassList.Contains("bit-drp-und"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-drp-lbl").TextContent.Trim());
        Assert.AreEqual(1, component.FindAll(".bit-drp-sin").Count);

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitDropdownParams<BitDropdownItem<string>, string> { Size = BitSize.Large } }));

        var root = component.Find(".bit-drp");

        Assert.IsFalse(root.ClassList.Contains("bit-drp-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-drp-lg"));
        Assert.AreEqual(0, component.FindAll(".bit-drp-lbl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-drp-sin").Count);
    }
}
