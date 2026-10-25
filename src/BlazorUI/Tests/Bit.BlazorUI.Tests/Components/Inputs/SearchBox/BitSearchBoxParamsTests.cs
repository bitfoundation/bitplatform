using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

/// <summary>
/// Covers the BitParams cascade of the SearchBox: what a BitSearchBoxParams fills in - its own parameters, the ones of
/// the text input base and the input base, and the ones of the component base alike - what it leaves alone because
/// the search box wrote it for itself, and that what the search box derives from its parameters (its classes, its
/// input attributes, its suggest policies) follows the cascaded values.
/// </summary>
[TestClass]
public class BitSearchBoxParamsTests : BunitTestContext
{
    private static readonly IReadOnlyList<string> Fruits =
    [
        "Apple", "Red Apple", "Blue Apple", "Green Apple", "Banana", "Orange", "Grape"
    ];

    // What a search box is about rather than how every search box behaves - its label, its hint, the rejection of its
    // own value, the search it is running, the affixes of its field and the items it suggests - is written per search
    // box, so it has no place on the params object; neither have the callbacks, the templates and the cascade itself.
    private static readonly string[] _notCascaded =
    [
        nameof(BitSearchBox.CascadingParameters),
        nameof(BitSearchBox.Description),
        nameof(BitSearchBox.ErrorMessage),
        nameof(BitSearchBox.Label),
        nameof(BitSearchBox.Loading),
        nameof(BitSearchBox.Prefix),
        nameof(BitSearchBox.Suffix),
        nameof(BitSearchBox.SuggestItems),
        nameof(BitSearchBox.SuggestItemsProvider),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitSearchBoxParams searchBoxParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { searchBoxParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSearchBox>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitSearchBoxParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSearchBox)}", BitSearchBoxParams.ParamName);
        Assert.AreEqual("BitParams.BitSearchBox", BitSearchBoxParams.ParamName);
    }

    [TestMethod]
    public void BitSearchBoxParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSearchBoxParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.IsInstanceOfType<BitInputBaseParams<string?>>(@params);
        Assert.AreEqual(BitSearchBoxParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSearchBoxShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitSearchBoxParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Underlined = true,
            FullWidth = true,
            NoBorder = true,
            Placeholder = "Cascaded placeholder",
            ClearButtonAriaLabel = "Cascaded clear",
            MaxLength = 12
        });

        var root = component.Find(".bit-srb");
        var input = component.Find(".bit-srb-inp");

        Assert.IsTrue(root.ClassList.Contains("bit-srb-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-flw"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-nbr"));
        Assert.AreEqual("Cascaded placeholder", input.GetAttribute("placeholder"));
        Assert.AreEqual("12", input.GetAttribute("maxlength"));
    }

    [TestMethod]
    public void BitSearchBoxDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitSearchBoxParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Placeholder = "Cascaded placeholder"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSearchBox.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(2, nameof(BitSearchBox.Placeholder), "Own placeholder");
        });

        var root = component.Find(".bit-srb");

        Assert.IsTrue(root.ClassList.Contains("bit-srb-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-srb-lg"));
        Assert.AreEqual("Own placeholder", component.Find(".bit-srb-inp").GetAttribute("placeholder"));

        // What the search box left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-srb-suc"));
    }

    [TestMethod]
    public void BitSearchBoxShouldTakeInheritedParametersFromTheCascade()
    {
        var component = RenderWithParams(new BitSearchBoxParams { ReadOnly = true, Required = true, MaxLength = 8 });

        // ReadOnly and Required are declared by the input base classes rather than by the search box, so they are
        // tracked in a set of their own, apart from the ones the search box declares.
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("readonly"));
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("required"));
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-req"));
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-rol"));
        Assert.AreEqual("8", component.Find(".bit-srb-inp").GetAttribute("maxlength"));
    }

    [TestMethod]
    public void BitSearchBoxOwnInheritedParametersShouldWinOverTheCascade()
    {
        var component = RenderWithParams(new BitSearchBoxParams { ReadOnly = true, Required = true, Immediate = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSearchBox.ReadOnly), false);
        });

        // The search box wrote ReadOnly="false" for itself, so the cascade must not turn it back on, while Required,
        // which it left unset, is still filled in from there.
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("readonly"));
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("required"));
    }

    [TestMethod]
    public void BitSearchBoxShouldTakeTheTextInputParametersFromTheCascade()
    {
        // AutoComplete, AutoFocus, Immediate, DebounceTime and ThrottleTime are declared by BitTextInputBase, one
        // more class whose parameters are taken out of the ParameterView before the sets below are filled.
        var component = RenderWithParams(new BitSearchBoxParams
        {
            AutoComplete = "off",
            AutoFocus = true,
            Immediate = true,
            DebounceTime = 0,
            ThrottleTime = 0,
        });

        var input = component.Find(".bit-srb-inp");
        var instance = component.FindComponent<BitSearchBox>().Instance;

        Assert.AreEqual("off", input.GetAttribute("autocomplete"));
        Assert.IsTrue(input.HasAttribute("autofocus"));
        Assert.IsTrue(instance.Immediate);

        // Immediate from the cascade is what lets the typed text be committed on every keystroke.
        input.Input("bit");

        Assert.AreEqual("bit", instance.Value);
    }

    [TestMethod]
    public void BitSearchBoxShouldTakeTheSuggestPoliciesFromTheCascade()
    {
        var component = RenderWithParams(new BitSearchBoxParams
        {
            Immediate = true,
            AutoFillSuggestItem = true,
            MinSuggestTriggerChars = 1,
            SuggestFilterFunction = (term, item) => item?.StartsWith("B", StringComparison.OrdinalIgnoreCase) is true,
            AnnouncementProvider = args => $"{args.SuggestItems.Count} found"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSearchBox.SuggestItems), Fruits);
        });

        Assert.AreEqual("both", component.Find(".bit-srb-inp").GetAttribute("aria-autocomplete"));

        component.Find(".bit-srb-inp").FocusIn();
        component.Find(".bit-srb-inp").Input("ap");

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count > 0);

        // The matching and the announcing of every search box under the cascade are its own policies.
        Assert.AreEqual(2, component.FindAll(".bit-srb-itm").Count);
        Assert.AreEqual("2 found", component.Find(".bit-srb-lvr").TextContent.Replace("​", string.Empty).Trim());
    }

    [TestMethod]
    public void BitSearchBoxShouldApplyTheCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitSearchBoxParams
        {
            Id = "cascaded-id",
            Class = "cascaded-class",
            Style = "margin: 1px;",
            Dir = BitDir.Rtl,
            Disabled = true,
            TabIndex = "3",
            AriaLabel = "Cascaded search",
            Visibility = BitVisibility.Hidden,
        });

        var root = component.Find(".bit-srb");
        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));

        Assert.AreEqual("Cascaded search", input.GetAttribute("aria-label"));
        Assert.AreEqual("3", input.GetAttribute("tabindex"));
        Assert.IsTrue(input.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitSearchBoxParamsShouldApplyClassesAndStyles()
    {
        var component = RenderWithParams(new BitSearchBoxParams
        {
            Classes = new BitSearchBoxClassStyles { Root = "custom-root", Input = "custom-input" },
            Styles = new BitSearchBoxClassStyles { Root = "color: red;", Input = "color: blue;" },
        });

        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-srb").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-srb-inp").ClassList.Contains("custom-input"));
        Assert.AreEqual("color: blue;", component.Find(".bit-srb-inp").GetAttribute("style"));
    }

    [TestMethod]
    public void BitSearchBoxParamsUpdateParametersShouldSetAllProperties()
    {
        Func<BitSearchBoxAnnouncementArgs, string?> announcementProvider = _ => "announced";
        Func<string?, string?, bool> filter = (_, _) => true;
        var clearButtonIcon = BitIconInfo.Bi("x-circle");
        var icon = BitIconInfo.Bi("search");
        var searchButtonIcon = BitIconInfo.Bi("arrow-right");

        var @params = new BitSearchBoxParams
        {
            AnnouncementProvider = announcementProvider,
            AriaDescription = "Cascaded aria description",
            AutoCapitalize = "none",
            AutoComplete = "off",
            AutoCorrect = false,
            AutoFocus = true,
            AutoFillSuggestItem = true,
            AutoSelectSuggestItem = true,
            Background = BitColorKind.Secondary,
            Classes = new BitSearchBoxClassStyles { Root = "cascaded-root" },
            ClearButtonAriaLabel = "Cascaded clear",
            ClearButtonIcon = clearButtonIcon,
            ClearButtonIconName = "Cancel",
            Color = BitColor.Warning,
            DebounceTime = 7,
            DisableAnimation = true,
            EnterKeyHint = BitEnterKeyHint.Go,
            FixedCalloutWidth = true,
            FixedIcon = true,
            FocusShortcut = "Control+K",
            FullWidth = true,
            HideClearButton = true,
            HideIcon = true,
            HighlightSuggestItems = true,
            Icon = icon,
            IconName = "Filter",
            Immediate = true,
            InputMode = BitInputMode.Search,
            LoadingAriaLabel = "Cascaded searching",
            LoadingText = "Cascaded loading",
            MaxLength = 20,
            MaxSuggestCount = 9,
            MinSuggestTriggerChars = 2,
            MinSuggestTriggerCharsText = "Type {0} more",
            Modeless = true,
            NoBorder = true,
            NoClearOnEscape = true,
            NoResultsText = "Cascaded nothing",
            NoWrapNavigation = true,
            Placeholder = "Cascaded placeholder",
            SearchButtonAriaLabel = "Cascaded search",
            SearchButtonIcon = searchButtonIcon,
            SearchButtonIconName = "Forward",
            SearchButtonText = "Go",
            SelectTextOnFocus = true,
            ShowSearchButton = true,
            ShowSuggestItemsOnFocus = true,
            Size = BitSize.Small,
            SpellCheck = false,
            Styles = new BitSearchBoxClassStyles { Root = "color: red;" },
            SuggestFailedText = "Cascaded failure",
            SuggestIgnoreDiacritics = true,
            SuggestFilterFunction = filter,
            SuggestItemsAriaLabel = "Cascaded suggestions",
            ThrottleTime = 8,
            Trim = true,
            Underlined = true,
            ReadOnly = true,
            Required = true,
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitSearchBox>().Instance;

        Assert.AreSame(announcementProvider, instance.AnnouncementProvider);
        Assert.AreEqual("Cascaded aria description", instance.AriaDescription);
        Assert.AreEqual("none", instance.AutoCapitalize);
        Assert.AreEqual("off", instance.AutoComplete);
        Assert.AreEqual((bool?)false, instance.AutoCorrect);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoFillSuggestItem);
        Assert.IsTrue(instance.AutoSelectSuggestItem);
        Assert.AreEqual(BitColorKind.Secondary, instance.Background);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual("Cascaded clear", instance.ClearButtonAriaLabel);
        Assert.AreSame(clearButtonIcon, instance.ClearButtonIcon);
        Assert.AreEqual("Cancel", instance.ClearButtonIconName);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual(7, instance.DebounceTime);
        Assert.IsTrue(instance.DisableAnimation);
        Assert.AreEqual(BitEnterKeyHint.Go, instance.EnterKeyHint);
        Assert.IsTrue(instance.FixedCalloutWidth);
        Assert.IsTrue(instance.FixedIcon);
        Assert.AreEqual("Control+K", instance.FocusShortcut);
        Assert.IsTrue(instance.FullWidth);
        Assert.IsTrue(instance.HideClearButton);
        Assert.IsTrue(instance.HideIcon);
        Assert.IsTrue(instance.HighlightSuggestItems);
        Assert.AreSame(icon, instance.Icon);
        Assert.AreEqual("Filter", instance.IconName);
        Assert.IsTrue(instance.Immediate);
        Assert.AreEqual(BitInputMode.Search, instance.InputMode);
        Assert.AreEqual("Cascaded searching", instance.LoadingAriaLabel);
        Assert.AreEqual("Cascaded loading", instance.LoadingText);
        Assert.AreEqual(20, instance.MaxLength);
        Assert.AreEqual(9, instance.MaxSuggestCount);
        Assert.AreEqual(2, instance.MinSuggestTriggerChars);
        Assert.AreEqual("Type {0} more", instance.MinSuggestTriggerCharsText);
        Assert.IsTrue(instance.Modeless);
        Assert.IsTrue(instance.NoBorder);
        Assert.IsTrue(instance.NoClearOnEscape);
        Assert.AreEqual("Cascaded nothing", instance.NoResultsText);
        Assert.IsTrue(instance.NoWrapNavigation);
        Assert.AreEqual("Cascaded placeholder", instance.Placeholder);
        Assert.AreEqual("Cascaded search", instance.SearchButtonAriaLabel);
        Assert.AreSame(searchButtonIcon, instance.SearchButtonIcon);
        Assert.AreEqual("Forward", instance.SearchButtonIconName);
        Assert.AreEqual("Go", instance.SearchButtonText);
        Assert.IsTrue(instance.SelectTextOnFocus);
        Assert.IsTrue(instance.ShowSearchButton);
        Assert.IsTrue(instance.ShowSuggestItemsOnFocus);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual((bool?)false, instance.SpellCheck);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreEqual("Cascaded failure", instance.SuggestFailedText);
        Assert.IsTrue(instance.SuggestIgnoreDiacritics);
        Assert.AreSame(filter, instance.SuggestFilterFunction);
        Assert.AreEqual("Cascaded suggestions", instance.SuggestItemsAriaLabel);
        Assert.AreEqual(8, instance.ThrottleTime);
        Assert.IsTrue(instance.Trim);
        Assert.IsTrue(instance.Underlined);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);

        // And the markup follows what was cascaded, not only the properties.
        var root = component.Find(".bit-srb");
        var input = component.Find(".bit-srb-inp");

        Assert.IsTrue(root.ClassList.Contains("bit-srb-bse"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-hic"));
        Assert.IsTrue(root.ClassList.Contains("bit-srb-sbt"));
        Assert.AreEqual("go", input.GetAttribute("enterkeyhint"));
        Assert.AreEqual("search", input.GetAttribute("inputmode"));
        Assert.AreEqual("none", input.GetAttribute("autocapitalize"));
        Assert.AreEqual("off", input.GetAttribute("autocorrect"));
        Assert.AreEqual("false", input.GetAttribute("spellcheck"));
        Assert.AreEqual("20", input.GetAttribute("maxlength"));
        Assert.AreEqual("Control+K", input.GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("Go", component.Find(".bit-srb-sbx").TextContent.Trim());
        Assert.AreEqual("Cascaded aria description", component.Find(".bit-srb-ard").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitSearchBoxParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Placeholder = "Params placeholder",
        };

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Placeholder, "Existing placeholder");
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual("Existing placeholder", instance.Placeholder);
    }

    [TestMethod]
    public void BitSearchBoxShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitSearchBoxParams());

        var root = component.Find(".bit-srb");
        var input = component.Find(".bit-srb-inp");

        // Nothing cascades, so the defaults hold: a plain search box with its icon, no color, background or size
        // class, and the defaults of the text it was given none of.
        Assert.IsFalse(root.ClassList.Contains("bit-srb-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-srb-bpr"));
        Assert.IsFalse(root.ClassList.Contains("bit-srb-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(1, component.FindAll(".bit-srb-iwp").Count);
        Assert.AreEqual("searchbox", input.GetAttribute("role"));
        Assert.AreEqual("search", input.GetAttribute("enterkeyhint"));
        Assert.IsFalse(input.HasAttribute("placeholder"));
        Assert.IsFalse(input.HasAttribute("maxlength"));
    }

    [TestMethod]
    public void BitSearchBoxShouldNotCascadeTheValueOfASingleField()
    {
        // Value and DefaultValue identify one field, so they are deliberately not part of the params class: a term
        // shared by every search box under the cascade is never what a consumer means.
        var properties = typeof(BitSearchBoxParams).GetProperties();

        Assert.IsNull(Array.Find(properties, p => p.Name == nameof(BitSearchBox.Value)));
        Assert.IsNull(Array.Find(properties, p => p.Name == nameof(BitSearchBox.DefaultValue)));
    }

    [TestMethod]
    public void BitSearchBoxParamsShouldCarryEveryParameterThatIsNotPerSearchBox()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks, templates, the cascade itself and what is written per search box are the ones
        // deliberately left out.
        var paramsProperties = typeof(BitSearchBoxParams).GetProperties().Select(p => p.Name).ToHashSet();

        var missing = typeof(BitSearchBox).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                          .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                          .Select(p => p.Name)
                                          .Where(n => _notCascaded.Contains(n) is false)
                                          .Where(n => paramsProperties.Contains(n) is false)
                                          .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }
}
