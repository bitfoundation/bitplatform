using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

[TestClass]
public class BitSearchBoxTests : BunitTestContext
{
    private static readonly List<string> Fruits =
    [
        "Apple", "Red Apple", "Blue Apple", "Green Apple", "Banana", "Orange", "Grape"
    ];

    private void SetupGetPropertyResult(string? result)
    {
        Context.JSInterop.Setup<string>("BitBlazorUI.Utils.getProperty", _ => true).SetResult(result!);
    }

    /// <summary>
    /// Focuses the input and waits for the suggest callout to actually open, because the component
    /// deliberately delays the opening to let Blazor render the items first.
    /// </summary>
    private static void OpenTheCallout(IRenderedComponent<BitSearchBox> component)
    {
        component.Find(".bit-srb-inp").FocusIn();

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");
    }

    /// <summary>
    /// Waits for the callout to be dismissed. The closing goes through an awaited JS interop call
    /// before the re-render, so the new markup is not necessarily there when the dispatch returns.
    /// </summary>
    private static void WaitForClosedCallout(IRenderedComponent<BitSearchBox> component)
    {
        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "false");
    }

    /// <summary>
    /// Waits for the virtual focus to land on the given suggest item. Moving the highlight scrolls the
    /// item into view through JS interop before the re-render, hence the wait.
    /// </summary>
    private static void WaitForSelectedItem(IRenderedComponent<BitSearchBox> component, int index)
    {
        component.WaitForState(() => component.FindAll(".bit-srb-itm")[index].GetAttribute("aria-selected") == "true");

        Assert.AreEqual(1, component.FindAll(".bit-srb-sel").Count);
    }



    [TestMethod,
        DataRow("Search"),
        DataRow("Filter")]
    public void SearchBoxPlaceholderMeetEnteredValue(string componentPlaceholder)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.Placeholder, componentPlaceholder));

        var input = component.Find(".bit-srb-inp");
        var inputPlaceholder = input.GetAttribute("placeholder");

        Assert.AreEqual(componentPlaceholder, inputPlaceholder);
    }

    [TestMethod,
        DataRow("Search"),
        DataRow("Closed Issue"),
        DataRow("fake value")]
    public void SearchBoxDefaultValueMeetEnteredValue(string value)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.Value, value));

        var input = component.Find(".bit-srb-inp");
        var inputValue = input.GetAttribute("value");

        Assert.AreEqual(value, inputValue);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxNoAnimationShouldHaveClassName(bool disableAnimation)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.DisableAnimation, disableAnimation));

        var searchBox = component.Find(".bit-srb");

        Assert.AreEqual(disableAnimation, searchBox.ClassList.Contains("bit-srb-nan"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxUnderlinedShouldHaveClassName(bool isUnderlined)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.Underlined, isUnderlined));

        var searchBox = component.Find(".bit-srb");

        Assert.AreEqual(isUnderlined, searchBox.ClassList.Contains("bit-srb-und"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxNoBorderShouldHaveClassName(bool noBorder)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.NoBorder, noBorder));

        Assert.AreEqual(noBorder, component.Find(".bit-srb").ClassList.Contains("bit-srb-nbr"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void SearchBoxFullWidthShouldHaveClassName(bool fullWidth)
    {
        var component = RenderComponent<BitSearchBox>(parameter => parameter.Add(p => p.FullWidth, fullWidth));

        Assert.AreEqual(fullWidth, component.Find(".bit-srb").ClassList.Contains("bit-srb-flw"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-srb-sm"),
        DataRow(BitSize.Medium, "bit-srb-md"),
        DataRow(BitSize.Large, "bit-srb-lg"),
        DataRow(null, null)]
    public void BitSearchBoxSizeShouldHaveCorrectClassName(BitSize? size, string? expectedClass)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.Size, size));

        var searchBox = component.Find(".bit-srb");

        if (expectedClass is null)
        {
            Assert.IsFalse(searchBox.ClassList.Contains("bit-srb-sm"));
            Assert.IsFalse(searchBox.ClassList.Contains("bit-srb-md"));
            Assert.IsFalse(searchBox.ClassList.Contains("bit-srb-lg"));
        }
        else
        {
            Assert.IsTrue(searchBox.ClassList.Contains(expectedClass));
        }
    }

    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-srb-bpr"),
        DataRow(BitColorKind.Secondary, "bit-srb-bse"),
        DataRow(BitColorKind.Tertiary, "bit-srb-btr"),
        DataRow(BitColorKind.Transparent, "bit-srb-btn")]
    public void BitSearchBoxBackgroundShouldHaveCorrectClassName(BitColorKind background, string expectedClass)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.Background, background));

        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow("Detailed label")]
    public void BitSearchBoxAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.AriaLabel, ariaLabel));

        var bitSearchBox = com.Find(".bit-srb-inp");

        Assert.IsTrue(bitSearchBox?.GetAttribute("aria-label")?.Equals(ariaLabel));
    }

    [TestMethod]
    public void BitSearchBoxLabelShouldBeConnectedToTheInput()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.Label, "Search products"));

        var label = component.Find("label.bit-srb-lbl");
        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual("Search products", label.TextContent.Trim());
        Assert.AreEqual(input.GetAttribute("id"), label.GetAttribute("for"));
        Assert.AreEqual(label.GetAttribute("id"), input.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitSearchBoxLabelTemplateShouldRender()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, "<span id='custom-label'>custom</span>");
        });

        Assert.IsNotNull(component.Find("#custom-label"));
        Assert.IsNotNull(component.Find(".bit-srb-inp").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitSearchBoxWithoutLabelShouldNotRenderLabelledBy()
    {
        var component = RenderComponent<BitSearchBox>();

        Assert.AreEqual(0, component.FindAll("label.bit-srb-lbl").Count);
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow("hello bit"),
        DataRow("hello world")
    ]
    public void BitSearchBoxShouldTakeDefaultValueWhenValueIsNotBound(string defaultValue)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual(defaultValue, input.GetAttribute("value"));
    }

    [TestMethod,
        DataRow("hello world", "hello bit"),
        DataRow("hello world", null)
    ]
    public void BitSearchBoxShouldIgnoreDefaultValueWhenValueIsBound(string value, string? defaultValue)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        var input = component.Find(".bit-srb-inp");

        // When Value is bound (controlled mode), DefaultValue is ignored.
        Assert.AreEqual(value, input.GetAttribute("value"));
    }

    [TestMethod,
        DataRow("hello world", true),
        DataRow("hello world", false)
    ]
    public void BitSearchBoxedMustShowSearchIconEvenHasValueWhenShowIconTrue(string value, bool fixedIcon)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.FixedIcon, fixedIcon);
        });

        var bitSearchBox = component.Find(".bit-srb");

        Assert.AreEqual(fixedIcon, bitSearchBox.ClassList.Contains("bit-srb-fic"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitSearchBoxHideIconShouldRemoveTheIcon(bool hideIcon)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.HideIcon, hideIcon));

        Assert.AreEqual(hideIcon ? 0 : 1, component.FindAll(".bit-srb-iwp").Count);
        Assert.AreEqual(hideIcon, component.Find(".bit-srb").ClassList.Contains("bit-srb-hic"));
    }

    [TestMethod]
    public void BitSearchBoxCustomIconNameShouldRender()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.IconName, "Filter"));

        Assert.IsTrue(component.Find(".bit-srb-iwp i").ClassList.Contains("bit-icon--Filter"));
    }

    [TestMethod,
        DataRow(null),
        DataRow("off"),
        DataRow("email")
    ]
    public void BitSearchBoxAutoCompleteTest(string autoComplete)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.AutoComplete, autoComplete);
            parameters.Add(p => p.IsEnabled, true);
        });

        var input = component.Find(".bit-srb-inp");

        if (autoComplete.HasValue())
        {
            Assert.IsTrue(input.HasAttribute("autocomplete"));
            Assert.AreEqual(autoComplete, input.GetAttribute("autocomplete"));
        }
        else
        {
            Assert.IsFalse(input.HasAttribute("autocomplete"));
        }
    }

    [TestMethod]
    public void BitSearchBoxShouldTurnOffTheNativeAutoCompleteWhenItHasSuggestItems()
    {
        // the native autofill dropdown of the browser would otherwise cover the suggest callout.
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SuggestItems, Fruits));

        Assert.AreEqual("off", component.Find(".bit-srb-inp").GetAttribute("autocomplete"));
    }

    [TestMethod]
    public void BitSearchBoxMaxLengthShouldRenderTheHtmlAttribute()
    {
        var withMaxLength = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.MaxLength, 10));
        Assert.AreEqual("10", withMaxLength.Find(".bit-srb-inp").GetAttribute("maxlength"));

        var withoutMaxLength = RenderComponent<BitSearchBox>();
        Assert.IsFalse(withoutMaxLength.Find(".bit-srb-inp").HasAttribute("maxlength"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitSearchBoxRequiredShouldRenderTheHtmlAttributeAndTheLabelMarker(bool required)
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Required, required);
            parameters.Add(p => p.Label, "Search");
        });

        Assert.AreEqual(required, component.Find(".bit-srb-inp").HasAttribute("required"));
        Assert.AreEqual(required, component.Find(".bit-srb").ClassList.Contains("bit-srb-req"));
    }

    [TestMethod]
    public void BitSearchBoxTrimShouldTrimTheBoundValue()
    {
        string? value = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Trim, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-srb-inp").Input("  bit  ");

        Assert.AreEqual("bit", value);
    }

    [TestMethod]
    public void BitSearchBoxWithoutTrimShouldKeepTheWhiteSpaces()
    {
        string? value = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-srb-inp").Input("  bit  ");

        Assert.AreEqual("  bit  ", value);
    }

    [TestMethod]
    public void BitSearchBoxPrefixAndSuffixShouldRender()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Prefix, "https://");
            parameters.Add(p => p.Suffix, ".com");
        });

        Assert.AreEqual("https://", component.Find(".bit-srb-pre").TextContent.Trim());
        Assert.AreEqual(".com", component.Find(".bit-srb-suf").TextContent.Trim());
    }



    #region clear button

    [TestMethod]
    public void BitSearchBoxClearButtonShouldOnlyRenderWhenThereIsAValue()
    {
        var empty = RenderComponent<BitSearchBox>();
        Assert.AreEqual(0, empty.FindAll(".bit-srb-cbt").Count);

        var filled = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.Value, "bit"));
        Assert.AreEqual(1, filled.FindAll(".bit-srb-cbt").Count);
    }

    [TestMethod]
    public void BitSearchBoxClearButtonShouldNotRenderInReadOnlyOrWhenHidden()
    {
        var readOnly = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.ReadOnly, true);
        });
        Assert.AreEqual(0, readOnly.FindAll(".bit-srb-cbt").Count);

        var hidden = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.HideClearButton, true);
        });
        Assert.AreEqual(0, hidden.FindAll(".bit-srb-cbt").Count);
    }

    [TestMethod]
    public void BitSearchBoxClearButtonShouldBeAccessibleAndOutOfTheTabOrder()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.ClearButtonAriaLabel, "Pak kardan");
        });

        var button = component.Find(".bit-srb-cbt");

        Assert.AreEqual("Pak kardan", button.GetAttribute("aria-label"));
        Assert.AreEqual("Pak kardan", button.GetAttribute("title"));
        Assert.AreEqual("-1", button.GetAttribute("tabindex"));
        // a focusable element must never be hidden from the accessibility tree.
        Assert.IsFalse(button.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSearchBoxClearButtonClickShouldClearTheValueAndInvokeOnClear()
    {
        string? value = "bit";
        var clearCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnClear, () => clearCount++);
        });

        component.Find(".bit-srb-cbt").Click();

        Assert.IsNull(value);
        Assert.AreEqual(1, clearCount);
        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt").Count);
    }

    [TestMethod]
    public async Task BitSearchBoxClearMethodShouldClearTheValueAndInvokeOnClear()
    {
        string? value = "bit";
        var clearCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnClear, () => clearCount++);
        });

        await component.InvokeAsync(() => component.Instance.Clear());

        Assert.IsNull(value);
        Assert.AreEqual(1, clearCount);
    }

    #endregion



    #region search button

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitSearchBoxSearchButtonShouldRenderWhenRequested(bool showSearchButton)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.ShowSearchButton, showSearchButton));

        Assert.AreEqual(showSearchButton ? 1 : 0, component.FindAll(".bit-srb-sbn").Count);
        Assert.AreEqual(showSearchButton, component.Find(".bit-srb").ClassList.Contains("bit-srb-ssb"));
    }

    [TestMethod]
    public void BitSearchBoxSearchButtonShouldBeAccessible()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.SearchButtonAriaLabel, "Jostoju");
        });

        var button = component.Find(".bit-srb-sbn");

        Assert.AreEqual("Jostoju", button.GetAttribute("aria-label"));
        Assert.AreEqual("Jostoju", button.GetAttribute("title"));
        Assert.IsFalse(button.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSearchBoxSearchButtonClickShouldInvokeOnSearch()
    {
        string? searched = null;
        var searchCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.OnSearch, v => { searched = v; searchCount++; });
        });

        component.Find(".bit-srb-sbn").Click();

        Assert.AreEqual(1, searchCount);
        Assert.AreEqual("bit", searched);
    }

    [TestMethod]
    public void BitSearchBoxDisabledButtonsShouldBeDisabled()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.ShowSearchButton, true);
        });

        Assert.IsTrue(component.Find(".bit-srb-sbn").HasAttribute("disabled"));
        Assert.IsTrue(component.Find(".bit-srb-cbt").HasAttribute("disabled"));
    }

    #endregion



    #region keyboard

    [TestMethod]
    public void BitSearchBoxEnterShouldInvokeOnSearchWithTheTypedValue()
    {
        SetupGetPropertyResult("bit blazor");

        string? searched = null;
        var searchCount = 0;
        string? value = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnSearch, v => { searched = v; searchCount++; });
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, searchCount);
        Assert.AreEqual("bit blazor", searched);
        Assert.AreEqual("bit blazor", value);
    }

    [TestMethod]
    public void BitSearchBoxEnterShouldInvokeOnSearchEvenWhenTheValueBindingIsOneWay()
    {
        SetupGetPropertyResult("bit");

        var searchCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.OnSearch, _ => searchCount++);
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, searchCount);
    }

    [TestMethod]
    public void BitSearchBoxEscapeShouldClearTheValueAndInvokeTheCallbacks()
    {
        string? value = "bit";
        var clearCount = 0;
        var escapeCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnClear, () => clearCount++);
            parameters.Add(p => p.OnEscape, () => escapeCount++);
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsNull(value);
        Assert.AreEqual(1, clearCount);
        Assert.AreEqual(1, escapeCount);
    }

    [TestMethod]
    public void BitSearchBoxEscapeShouldNotInvokeOnClearWhenThereIsNoValue()
    {
        var clearCount = 0;
        var escapeCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.OnClear, () => clearCount++);
            parameters.Add(p => p.OnEscape, () => escapeCount++);
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, clearCount);
        Assert.AreEqual(1, escapeCount);
    }

    [TestMethod]
    public void BitSearchBoxNoClearOnEscapeShouldKeepTheValue()
    {
        string? value = "bit";
        var clearCount = 0;
        var escapeCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.NoClearOnEscape, true);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnClear, () => clearCount++);
            parameters.Add(p => p.OnEscape, () => escapeCount++);
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("bit", value);
        Assert.AreEqual(0, clearCount);
        Assert.AreEqual(1, escapeCount);
    }

    [TestMethod]
    public void BitSearchBoxReadOnlyEscapeShouldNotClearTheValue()
    {
        string? value = "bit";

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual("bit", value);
    }

    [TestMethod]
    public void BitSearchBoxOnKeyDownAndOnKeyUpShouldBeInvoked()
    {
        var downCount = 0;
        var upCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.OnKeyDown, _ => downCount++);
            parameters.Add(p => p.OnKeyUp, _ => upCount++);
        });

        var input = component.Find(".bit-srb-inp");
        input.KeyDown(new KeyboardEventArgs { Key = "a" });
        input.KeyUp(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual(1, downCount);
        Assert.AreEqual(1, upCount);
    }

    [TestMethod]
    public void BitSearchBoxDisabledShouldNotRaiseTheKeyboardCallbacks()
    {
        var downCount = 0;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnKeyDown, _ => downCount++);
        });

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual(0, downCount);
    }

    [TestMethod]
    public void BitSearchBoxArrowKeysShouldMoveTheHighlightedSuggestItem()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        var input = component.Find(".bit-srb-inp");
        input.Input("apple");

        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-sel").Count);

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.AreEqual("true", component.FindAll(".bit-srb-itm")[0].GetAttribute("aria-selected"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.AreEqual("true", component.FindAll(".bit-srb-itm")[1].GetAttribute("aria-selected"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual("true", component.FindAll(".bit-srb-itm")[0].GetAttribute("aria-selected"));

        // wraps around to the last item
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual("true", component.FindAll(".bit-srb-itm")[3].GetAttribute("aria-selected"));

        // wraps around to the first item
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.AreEqual("true", component.FindAll(".bit-srb-itm")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitSearchBoxEnterShouldPickTheHighlightedSuggestItem()
    {
        string? value = null;
        string? selected = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnSuggestItemSelect, i => selected = i);
        });

        var input = component.Find(".bit-srb-inp");
        input.Input("apple");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("Apple", value);
        Assert.AreEqual("Apple", selected);
    }

    [TestMethod]
    public void BitSearchBoxAutoSelectSuggestItemShouldHighlightTheFirstItem()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoSelectSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        Assert.AreEqual("true", component.FindAll(".bit-srb-itm")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitSearchBoxHomeAndEndShouldJumpToTheEdgesOfTheSuggestList()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        // Home & End only take over the list navigation while the callout is open and an item is
        // virtually focused, so the input is focused and an arrow key is pressed first.
        OpenTheCallout(component);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        WaitForSelectedItem(component, 0);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "End" });
        WaitForSelectedItem(component, 3);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Home" });
        WaitForSelectedItem(component, 0);

        Assert.AreEqual(1, component.FindAll(".bit-srb-sel").Count);
    }

    [TestMethod]
    public void BitSearchBoxHomeAndEndShouldBeIgnoredWhileNoItemIsHighlighted()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        OpenTheCallout(component);

        // they must keep their default behavior of moving the caret inside the input.
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "End" });
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.AreEqual(0, component.FindAll(".bit-srb-sel").Count);
    }

    [TestMethod]
    public void BitSearchBoxTabShouldCloseTheSuggestCallout()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        OpenTheCallout(component);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        WaitForClosedCallout(component);
    }

    [TestMethod]
    public void BitSearchBoxEscapeShouldCloseTheCalloutBeforeClearingTheValue()
    {
        string? value = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-srb-inp").Input("apple");

        OpenTheCallout(component);

        // the first escape only dismisses the callout...
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        WaitForClosedCallout(component);
        Assert.AreEqual("apple", value);

        // ...and the second one clears the value.
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        component.WaitForState(() => value is null);
    }

    [TestMethod]
    public void BitSearchBoxOverlayClickShouldCloseTheSuggestCallout()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        OpenTheCallout(component);

        Assert.AreEqual("display:block;", component.Find(".bit-srb-ovl").GetAttribute("style"));

        component.Find(".bit-srb-ovl").Click();

        WaitForClosedCallout(component);
        Assert.AreEqual("display:none;", component.Find(".bit-srb-ovl").GetAttribute("style"));
    }

    [TestMethod]
    public void BitSearchBoxActiveDescendantShouldPointToTheHighlightedItem()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        OpenTheCallout(component);

        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-activedescendant"));

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        WaitForSelectedItem(component, 0);

        Assert.AreEqual(component.FindAll(".bit-srb-itm")[0].GetAttribute("id"),
                        component.Find(".bit-srb-inp").GetAttribute("aria-activedescendant"));
    }

    #endregion



    #region suggest items

    [TestMethod]
    public void BitSearchBoxWithoutSuggestSourceShouldNotRenderTheCallout()
    {
        var component = RenderComponent<BitSearchBox>();

        Assert.AreEqual(0, component.FindAll(".bit-srb-cal").Count);
        Assert.AreEqual("searchbox", component.Find(".bit-srb-inp").GetAttribute("role"));
    }

    [TestMethod]
    public void BitSearchBoxWithSuggestItemsShouldExposeTheComboboxSemantics()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SuggestItems, Fruits));

        var input = component.Find(".bit-srb-inp");
        var listbox = component.Find(".bit-srb-scn");

        Assert.AreEqual("combobox", input.GetAttribute("role"));
        Assert.AreEqual("list", input.GetAttribute("aria-autocomplete"));
        Assert.AreEqual("listbox", input.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", input.GetAttribute("aria-expanded"));
        Assert.AreEqual(listbox.GetAttribute("id"), input.GetAttribute("aria-controls"));
        Assert.AreEqual("listbox", listbox.GetAttribute("role"));
        Assert.AreEqual("Suggestions", listbox.GetAttribute("aria-label"));
        Assert.IsFalse(input.HasAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitSearchBoxSuggestItemsShouldExposeThePositionInTheSet()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        var items = component.FindAll(".bit-srb-itm");

        Assert.AreEqual(4, items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            Assert.AreEqual("option", items[i].GetAttribute("role"));
            Assert.AreEqual("-1", items[i].GetAttribute("tabindex"));
            Assert.AreEqual("4", items[i].GetAttribute("aria-setsize"));
            Assert.AreEqual((i + 1).ToString(), items[i].GetAttribute("aria-posinset"));
        }
    }

    [TestMethod]
    public void BitSearchBoxShouldNotSearchBeforeTheMinSuggestTriggerChars()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 3);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        var input = component.Find(".bit-srb-inp");

        input.Input("ap");
        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);

        input.Input("app");
        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxZeroMinSuggestTriggerCharsShouldSearchWithAnEmptyTerm()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MaxSuggestCount, 0);
            parameters.Add(p => p.MinSuggestTriggerChars, 0);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        var input = component.Find(".bit-srb-inp");

        input.Input("apple");
        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);

        // an empty term now matches everything instead of clearing the list.
        input.Input(string.Empty);
        Assert.AreEqual(Fruits.Count, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxMaxSuggestCountShouldLimitTheRenderedItems()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MaxSuggestCount, 2);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        Assert.AreEqual(2, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxNonPositiveMaxSuggestCountShouldNotLimitTheRenderedItems()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MaxSuggestCount, 0);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("a");

        Assert.AreEqual(Fruits.Count(f => f.Contains('a', StringComparison.OrdinalIgnoreCase)),
                        component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxSuggestFilterFunctionShouldReplaceTheDefaultMatching()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.SuggestFilterFunction,
                (string? term, string? item) => item?.StartsWith(term ?? string.Empty, StringComparison.OrdinalIgnoreCase) ?? false);
        });

        component.Find(".bit-srb-inp").Input("apple");

        var items = component.FindAll(".bit-srb-itm");

        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("Apple", items[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxSuggestItemClickShouldSetTheValueAndInvokeTheCallbacks()
    {
        string? value = null;
        string? selected = null;
        string? searched = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnSearch, v => searched = v);
            parameters.Add(p => p.OnSuggestItemSelect, i => selected = i);
        });

        component.Find(".bit-srb-inp").Input("apple");
        component.FindAll(".bit-srb-itm")[1].Click();

        Assert.AreEqual("Red Apple", value);
        Assert.AreEqual("Red Apple", selected);
        Assert.AreEqual("Red Apple", searched);
    }

    [TestMethod]
    public void BitSearchBoxReadOnlyShouldNotAllowPickingASuggestItem()
    {
        string? value = "apple";

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var items = component.FindAll(".bit-srb-itm");
        if (items.Count > 0)
        {
            items[0].Click();
        }

        Assert.AreEqual("apple", value);
    }

    [TestMethod]
    public void BitSearchBoxDuplicateSuggestItemsShouldHighlightOnlyTheNavigatedOne()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, new List<string> { "Apple", "Apple", "Apple" });
        });

        var input = component.Find(".bit-srb-inp");
        input.Input("apple");
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var items = component.FindAll(".bit-srb-itm");

        Assert.AreEqual(3, items.Count);
        Assert.AreEqual(1, component.FindAll(".bit-srb-sel").Count);
        Assert.AreEqual("false", items[0].GetAttribute("aria-selected"));
        Assert.AreEqual("true", items[1].GetAttribute("aria-selected"));
        Assert.AreEqual("false", items[2].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitSearchBoxHighlightSuggestItemsShouldMarkTheMatchedText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.HighlightSuggestItems, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, new List<string> { "Red Apple" });
        });

        component.Find(".bit-srb-inp").Input("app");

        var marks = component.FindAll(".bit-srb-itm mark");

        Assert.AreEqual(1, marks.Count);
        Assert.AreEqual("App", marks[0].TextContent);
        Assert.AreEqual("Red Apple", component.Find(".bit-srb-itm").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxWithoutHighlightShouldNotRenderAnyMark()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, new List<string> { "Red Apple" });
        });

        component.Find(".bit-srb-inp").Input("app");

        Assert.AreEqual(0, component.FindAll(".bit-srb-itm mark").Count);
    }

    [TestMethod]
    public void BitSearchBoxNoResultsTextShouldRenderOnlyAfterAFruitlessSearch()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.NoResultsText, "Nothing here");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        Assert.AreEqual(0, component.FindAll(".bit-srb-nrs").Count);

        var input = component.Find(".bit-srb-inp");

        input.Input("apple");
        Assert.AreEqual(0, component.FindAll(".bit-srb-nrs").Count);

        input.Input("zzz");
        Assert.AreEqual("Nothing here", component.Find(".bit-srb-nrs").TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxCalloutHeaderAndFooterTemplatesShouldRender()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.CalloutHeaderTemplate, "<span id='cal-header'>header</span>");
            parameters.Add(p => p.CalloutFooterTemplate, "<span id='cal-footer'>footer</span>");
        });

        Assert.IsNotNull(component.Find("#cal-header"));
        Assert.IsNotNull(component.Find("#cal-footer"));
    }

    [TestMethod]
    public void BitSearchBoxSuggestItemTemplateShouldReplaceTheDefaultItemContent()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, new List<string> { "Apple" });
            parameters.Add(p => p.SuggestItemTemplate, item => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-item");
                builder.AddContent(2, $"[{item}]");
                builder.CloseElement();
            });
        });

        component.Find(".bit-srb-inp").Input("app");

        Assert.AreEqual("[Apple]", component.Find(".bit-srb-itm .custom-item").TextContent);
    }

    [TestMethod]
    public void BitSearchBoxModelessShouldNotRenderTheOverlay()
    {
        var withOverlay = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SuggestItems, Fruits));
        Assert.AreEqual(1, withOverlay.FindAll(".bit-srb-ovl").Count);

        var modeless = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Modeless, true);
            parameters.Add(p => p.SuggestItems, Fruits);
        });
        Assert.AreEqual(0, modeless.FindAll(".bit-srb-ovl").Count);
    }

    [TestMethod]
    public void BitSearchBoxSuggestItemsProviderShouldRenderItems()
    {
        string? receivedSearchTerm = null;
        int receivedTake = 0;

        var component = RenderComponent<BitSearchBox>(p =>
        {
            p.Add(x => x.Immediate, true);
            p.Add(x => x.MinSuggestTriggerChars, 3);
            p.Add(x => x.SuggestItemsProvider, (BitSearchBoxSuggestItemsProviderRequest req) =>
            {
                receivedSearchTerm = req.SearchTerm;
                receivedTake = req.Take;
                return req.SearchTerm == "app"
                    ? ValueTask.FromResult<IEnumerable<string>>(new List<string> { "apple", "application" })
                    : ValueTask.FromResult<IEnumerable<string>>(new List<string> { "banana" });
            });
        });

        var input = component.Find(".bit-srb-inp");
        input.Input("app");

        var items = component.FindAll(".bit-srb-itm");

        Assert.AreEqual("app", receivedSearchTerm);
        Assert.AreEqual(5, receivedTake);
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("apple", items[0].TextContent.Trim());
        Assert.AreEqual("application", items[1].TextContent.Trim());

        input.Input("ban");

        items = component.FindAll(".bit-srb-itm");

        Assert.AreEqual("ban", receivedSearchTerm);
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("banana", items[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxSuggestItemsProviderShouldRespectMaxSuggestCount()
    {
        var component = RenderComponent<BitSearchBox>(p =>
        {
            p.Add(x => x.Immediate, true);
            p.Add(x => x.MaxSuggestCount, 2);
            p.Add(x => x.MinSuggestTriggerChars, 1);
            p.Add(x => x.SuggestItemsProvider, (BitSearchBoxSuggestItemsProviderRequest req) =>
                ValueTask.FromResult<IEnumerable<string>>(new List<string> { "a", "b", "c", "d" }));
        });

        component.Find(".bit-srb-inp").Input("a");

        Assert.AreEqual(2, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxSuggestItemsProviderFailureShouldNotCrashTheComponent()
    {
        var component = RenderComponent<BitSearchBox>(p =>
        {
            p.Add(x => x.Immediate, true);
            p.Add(x => x.MinSuggestTriggerChars, 1);
            p.Add(x => x.NoResultsText, "Nothing here");
            p.Add(x => x.SuggestItemsProvider, (BitSearchBoxSuggestItemsProviderRequest req) =>
                ValueTask.FromException<IEnumerable<string>>(new InvalidOperationException("boom")));
        });

        component.Find(".bit-srb-inp").Input("a");

        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);
        Assert.AreEqual("Nothing here", component.Find(".bit-srb-nrs").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitSearchBoxDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitSearchBox>(p =>
        {
            p.Add(x => x.SuggestItemsProvider, (BitSearchBoxSuggestItemsProviderRequest req) =>
                ValueTask.FromResult<IEnumerable<string>>(new List<string> { "a", "b" }));
        });

        await component.Instance.DisposeAsync();
    }

    #endregion



    #region validation

    [TestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitSearchBoxValidationFormTest(string value)
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitSearchBoxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Immediate, true);
        });

        var isValid = value == "test@bit.com" || value == "test@bit-components.com";

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(component.Instance.ValidCount, isValid ? 1 : 0);
        Assert.AreEqual(component.Instance.InvalidCount, isValid ? 0 : 1);

        var input = component.Find("input.bit-srb-inp");
        if (isValid)
        {
            input.Input("bit.com");
        }
        else
        {
            input.Input("test@bit.com");
        }

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [TestMethod,
        DataRow(null),
        DataRow("abc123"),
        DataRow("test@bit-components.com"),
        DataRow("test@bit.com"),
    ]
    public void BitSearchBoxValidationInvalidHtmlAttributeTest(string value)
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitSearchBoxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Immediate, true);
        });

        var isInvalid = value != "test@bit.com" && value != "test@bit-components.com";

        var input = component.Find("input");
        Assert.IsFalse(input.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, input.HasAttribute("aria-invalid"));
        if (input.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        }

        if (isInvalid)
        {
            input.Input("test@bit.com");
            Assert.IsFalse(input.HasAttribute("aria-invalid"));
        }
        else
        {
            input.Input("bit-components");
            Assert.IsTrue(input.HasAttribute("aria-invalid"));
        }
    }

    [TestMethod,
        DataRow("abc123"),
        DataRow("test@bit.com")
    ]
    public void BitSearchBoxValidationInvalidCssClassTest(string value)
    {
        var component = RenderComponent<BitSearchBoxValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitSearchBoxTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.Immediate, true);
        });

        var isInvalid = value != "test@bit.com";

        var bitSearchBox = component.Find(".bit-srb");

        Assert.IsFalse(bitSearchBox.ClassList.Contains("bit-inv"));

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(isInvalid, bitSearchBox.ClassList.Contains("bit-inv"));

        var input = component.Find("input");
        if (isInvalid)
        {
            input.Input("test@bit.com");
        }
        else
        {
            input.Input("abc123");
        }

        Assert.AreEqual(isInvalid is false, bitSearchBox.ClassList.Contains("bit-inv"));
    }

    #endregion
}
