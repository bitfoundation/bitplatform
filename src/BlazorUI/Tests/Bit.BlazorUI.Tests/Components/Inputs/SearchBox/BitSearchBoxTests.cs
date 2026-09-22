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
    private static readonly IReadOnlyList<string> Fruits =
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

    /// <summary>
    /// Types into the input of a focused search box, which is what the component needs to consider the
    /// suggest list visible and therefore worth opening and announcing.
    /// </summary>
    private static void FocusAndType(IRenderedComponent<BitSearchBox> component, string text)
    {
        component.Find(".bit-srb-inp").FocusIn();
        component.Find(".bit-srb-inp").Input(text);
    }

    /// <summary>
    /// Reads the text of the live region without the zero width space the component alternates to
    /// force screen readers to re-announce an otherwise identical message.
    /// </summary>
    private static string GetAnnouncement(IRenderedComponent<BitSearchBox> component)
    {
        return component.Find(".bit-srb-lvr").TextContent.Replace("\u200B", string.Empty).Trim();
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
    // The asterisk of a required label is a ::after pseudo-element driven by the bit-srb-req class,
    // so the class on the root is as far as this can assert; the marker itself is not in the DOM.
    public void BitSearchBoxRequiredShouldRenderTheHtmlAttributeAndTheRootClass(bool required)
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

    [TestMethod]
    public void BitSearchBoxPrefixAndSuffixTemplatesShouldReplaceTheTextVariants()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Prefix, "https://");
            parameters.Add(p => p.Suffix, ".com");
            parameters.Add(p => p.PrefixTemplate, "<span id='pre-tpl'>pre</span>");
            parameters.Add(p => p.SuffixTemplate, "<span id='suf-tpl'>suf</span>");
        });

        Assert.IsNotNull(component.Find("#pre-tpl"));
        Assert.IsNotNull(component.Find("#suf-tpl"));
        Assert.AreEqual(0, component.FindAll(".bit-srb-pre").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-suf").Count);
    }

    [TestMethod,
        DataRow(null, "search"),
        DataRow(BitEnterKeyHint.Go, "go"),
        DataRow(BitEnterKeyHint.Done, "done"),
        DataRow(BitEnterKeyHint.Enter, "enter"),
        DataRow(BitEnterKeyHint.Next, "next"),
        DataRow(BitEnterKeyHint.Previous, "previous"),
        DataRow(BitEnterKeyHint.Send, "send"),
        DataRow(BitEnterKeyHint.Search, "search")]
    public void BitSearchBoxEnterKeyHintShouldRenderTheHtmlAttribute(BitEnterKeyHint? enterKeyHint, string expected)
    {
        // it defaults to search because pressing enter in a search box always runs a search.
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.EnterKeyHint, enterKeyHint));

        Assert.AreEqual(expected, component.Find(".bit-srb-inp").GetAttribute("enterkeyhint"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(true, "true"),
        DataRow(false, "false")]
    public void BitSearchBoxSpellCheckShouldRenderTheHtmlAttribute(bool? spellCheck, string? expected)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SpellCheck, spellCheck));

        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual(expected is not null, input.HasAttribute("spellcheck"));
        Assert.AreEqual(expected, input.GetAttribute("spellcheck"));
    }

    [TestMethod,
        DataRow(BitInputMode.Search, "search"),
        DataRow(BitInputMode.Numeric, "numeric"),
        DataRow(null, null)]
    public void BitSearchBoxInputModeShouldRenderTheHtmlAttribute(BitInputMode? inputMode, string? expected)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.InputMode, inputMode));

        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual(expected is not null, input.HasAttribute("inputmode"));
        Assert.AreEqual(expected, input.GetAttribute("inputmode"));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-srb-pri"),
        DataRow(BitColor.Secondary, "bit-srb-sec"),
        DataRow(BitColor.Error, "bit-srb-err"),
        DataRow(BitColor.Success, "bit-srb-suc"),
        DataRow(null, "bit-srb-pri")]
    public void BitSearchBoxColorShouldHaveCorrectClassName(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.Color, color));

        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitSearchBoxStylesAndClassesShouldReachTheInternalParts()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Label, "Search");
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.Classes, new BitSearchBoxClassStyles
            {
                Root = "root-cls",
                Label = "label-cls",
                Input = "input-cls",
                ClearButton = "clear-cls",
                SearchButton = "search-cls",
                InputContainer = "container-cls"
            });
            parameters.Add(p => p.Styles, new BitSearchBoxClassStyles
            {
                Root = "z-index: 1;",
                Input = "color: red;"
            });
        });

        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("root-cls"));
        Assert.IsTrue(component.Find("label.bit-srb-lbl").ClassList.Contains("label-cls"));
        Assert.IsTrue(component.Find(".bit-srb-cnt").ClassList.Contains("container-cls"));
        Assert.IsTrue(component.Find(".bit-srb-inp").ClassList.Contains("input-cls"));
        Assert.IsTrue(component.Find(".bit-srb-cbt").ClassList.Contains("clear-cls"));
        Assert.IsTrue(component.Find(".bit-srb-sbn").ClassList.Contains("search-cls"));

        StringAssert.Contains(component.Find(".bit-srb").GetAttribute("style"), "z-index: 1;");
        StringAssert.Contains(component.Find(".bit-srb-inp").GetAttribute("style"), "color: red;");
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
    public void BitSearchBoxClearButtonShouldFollowTheTypedTextWithoutImmediate()
    {
        // without Immediate the model is only updated on change, but the field visibly holds text
        // from the very first keystroke, so the clear button (and the icon collapsing state) must follow it.
        var component = RenderComponent<BitSearchBox>();

        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt").Count);
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-hvl"));

        component.Find(".bit-srb-inp").Input("bit");

        Assert.AreEqual(1, component.FindAll(".bit-srb-cbt").Count);
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-hvl"));

        component.Find(".bit-srb-inp").Input(string.Empty);

        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt").Count);
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-hvl"));
    }

    [TestMethod]
    public void BitSearchBoxClearButtonShouldDisappearAfterClearingTheUncommittedText()
    {
        var component = RenderComponent<BitSearchBox>();

        component.Find(".bit-srb-inp").Input("bit");
        Assert.AreEqual(1, component.FindAll(".bit-srb-cbt").Count);

        component.Find(".bit-srb-cbt").Click();

        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt").Count);
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-hvl"));
    }

    [TestMethod]
    public void BitSearchBoxReadOnlyShouldNotTrackTheTypedText()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.ReadOnly, true));

        component.Find(".bit-srb-inp").Input("bit");

        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt").Count);
    }

    [TestMethod]
    public void BitSearchBoxCustomClearAndSearchButtonIconsShouldRender()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.ClearButtonIconName, "RemoveFilter");
            parameters.Add(p => p.SearchButtonIconName, "PageListFilter");
        });

        Assert.IsTrue(component.Find(".bit-srb-cbt i").ClassList.Contains("bit-icon--RemoveFilter"));
        Assert.IsTrue(component.Find(".bit-srb-sbn i").ClassList.Contains("bit-icon--PageListFilter"));
    }

    [TestMethod]
    public void BitSearchBoxClearAndSearchButtonTemplatesShouldReplaceTheIcons()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "bit");
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.ClearButtonTemplate, "<span id='clear-tpl'>x</span>");
            parameters.Add(p => p.SearchButtonTemplate, "<span id='search-tpl'>go</span>");
        });

        Assert.IsNotNull(component.Find(".bit-srb-cbt #clear-tpl"));
        Assert.IsNotNull(component.Find(".bit-srb-sbn #search-tpl"));
        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt i").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-sbn i").Count);
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
        WaitForSelectedItem(component, 0);

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        WaitForSelectedItem(component, 1);

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        WaitForSelectedItem(component, 0);

        // wraps around to the last item
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        WaitForSelectedItem(component, 3);

        // wraps around to the first item
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        WaitForSelectedItem(component, 0);
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
        WaitForSelectedItem(component, 0);

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

        WaitForSelectedItem(component, 0);
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
    public void BitSearchBoxPageKeysShouldJumpAWholePageOfSuggestItems()
    {
        var many = Enumerable.Range(1, 12).Select(i => $"Apple {i:00}").ToList();

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MaxSuggestCount, 0);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, many);
        });

        component.Find(".bit-srb-inp").Input("apple");

        OpenTheCallout(component);

        // page down starts the highlight at the top of the list...
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        WaitForSelectedItem(component, 4);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        WaitForSelectedItem(component, 9);

        // ...and clamps at the last item instead of wrapping around like the arrow keys do.
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        WaitForSelectedItem(component, 11);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        WaitForSelectedItem(component, 6);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        WaitForSelectedItem(component, 1);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        WaitForSelectedItem(component, 0);
    }

    [TestMethod]
    public void BitSearchBoxPageKeysShouldBeIgnoredWhileTheCalloutIsClosed()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.AreEqual(0, component.FindAll(".bit-srb-sel").Count);
    }

    [TestMethod]
    public void BitSearchBoxAltArrowKeysShouldOpenAndCloseTheSuggestCallout()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        // alt + up dismisses the list without touching the value...
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });
        WaitForClosedCallout(component);

        // ...and alt + down brings it back without highlighting anything.
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

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

    [TestMethod]
    public void BitSearchBoxFocusShortcutShouldBeAdvertisedOnTheInputAndListenedFor()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.FocusShortcut, "Control+K Meta+K");
        });

        // The very same string drives the listener and tells assistive technologies about it.
        Assert.AreEqual("Control+K Meta+K", component.Find(".bit-srb-inp").GetAttribute("aria-keyshortcuts"));

        var registration = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.SearchBox.registerShortcut");

        Assert.AreEqual(component.Find(".bit-srb-inp").Id, registration.Arguments[0]);
        Assert.AreEqual("Control+K Meta+K", registration.Arguments[1]);
    }

    [TestMethod]
    public void BitSearchBoxWithoutAFocusShortcutShouldNeitherAdvertiseNorRegisterOne()
    {
        var component = RenderComponent<BitSearchBox>();

        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-keyshortcuts"));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.SearchBox.registerShortcut"));
    }

    [TestMethod]
    public void BitSearchBoxChangingTheFocusShortcutShouldTakeThePreviousOneBackOff()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.FocusShortcut, "Control+K");
        });

        component.Render(parameters => parameters.Add(p => p.FocusShortcut, "Alt+S"));

        // The listener sits on the document, so the second registration would otherwise leave the
        // first one armed for the rest of the life of the page.
        var registrations = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.SearchBox.registerShortcut").ToList();

        Assert.AreEqual(2, registrations.Count);
        Assert.AreEqual("Alt+S", registrations[1].Arguments[1]);

        component.Render(parameters => parameters.Add(p => p.FocusShortcut, (string?)null));

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.SearchBox.unregisterShortcut"));
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
    public async Task BitSearchBoxReadOnlyShouldNotAllowPickingASuggestItem()
    {
        string? value = "apple";

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // read-only blocks editing, not browsing, so the suggestions are rendered as usual.
        await component.InvokeAsync(() => component.Instance.ShowSuggestItems());
        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 4);

        component.FindAll(".bit-srb-itm")[0].Click();

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
        WaitForSelectedItem(component, 0);

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        WaitForSelectedItem(component, 1);

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
    public void BitSearchBoxCalloutShouldCarryTheDirectionAndTheAnimationOptOutOfTheComponent()
    {
        // the callout is rendered outside of the root element, so it inherits neither of them on its own.
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.DisableAnimation, true);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        var callout = component.Find(".bit-srb-cal");

        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-srb-nan"));

        var ltr = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SuggestItems, Fruits));

        Assert.IsFalse(ltr.Find(".bit-srb-cal").HasAttribute("dir"));
        Assert.IsFalse(ltr.Find(".bit-srb-cal").ClassList.Contains("bit-srb-nan"));
    }

    [TestMethod]
    public void BitSearchBoxReopeningTheCalloutShouldNotRestoreTheOldHighlight()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        WaitForSelectedItem(component, 0);

        // the overlay dismisses the callout without going through any of the keyboard handlers.
        component.Find(".bit-srb-ovl").Click();
        WaitForClosedCallout(component);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        Assert.AreEqual(0, component.FindAll(".bit-srb-sel").Count);
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-activedescendant"));
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
        // the provider is handed the MaxSuggestCount of the component, which was left at its default.
        Assert.AreEqual(5, component.Instance.MaxSuggestCount);
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
    public void BitSearchBoxSuggestItemsProviderFailureShouldNotCrashNorBeToldAsAnEmptyResult()
    {
        var component = RenderComponent<BitSearchBox>(p =>
        {
            p.Add(x => x.Immediate, true);
            p.Add(x => x.MinSuggestTriggerChars, 1);
            p.Add(x => x.NoResultsText, "Nothing here");
            p.Add(x => x.SuggestItemsProvider, (BitSearchBoxSuggestItemsProviderRequest req) =>
                ValueTask.FromException<IEnumerable<string>>(new InvalidOperationException("boom")));
        });

        FocusAndType(component, "a");

        component.WaitForState(() => component.Find(".bit-srb-lvr").TextContent.Contains("could not be loaded"));

        // A faulted ValueTask is a search that could not run, so the message for a search that ran and
        // found nothing must not stand in for it - the live region says what actually happened.
        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-nrs").Count);
    }

    [TestMethod]
    public async Task BitSearchBoxLoadingTemplateAndTextShouldRenderWhileTheProviderIsRunning()
    {
        var tcs = new TaskCompletionSource<IEnumerable<string>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var component = RenderComponent<BitSearchBox>(p =>
        {
            p.Add(x => x.Immediate, true);
            p.Add(x => x.MinSuggestTriggerChars, 1);
            p.Add(x => x.LoadingText, "Searching...");
            p.Add(x => x.SuggestItemsProvider, (BitSearchBoxSuggestItemsProviderRequest req) => new(tcs.Task));
        });

        FocusAndType(component, "a");

        component.WaitForState(() => component.FindAll(".bit-srb-lod").Count == 1);

        Assert.AreEqual("Searching...", component.Find(".bit-srb-lod").TextContent.Trim());
        Assert.AreEqual("true", component.Find(".bit-srb-scn").GetAttribute("aria-busy"));
        // the visible indicator is muted for screen readers, the live region is what reports the state.
        Assert.AreEqual("true", component.Find(".bit-srb-lod").GetAttribute("aria-hidden"));
        Assert.AreEqual("Searching...", GetAnnouncement(component));

        await component.InvokeAsync(() => tcs.SetResult(["Apple"]));

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 1);

        Assert.AreEqual(0, component.FindAll(".bit-srb-lod").Count);
        Assert.IsFalse(component.Find(".bit-srb-scn").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitSearchBoxNoResultsTemplateShouldRenderInsteadOfTheText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.NoResultsText, "Nothing here");
            parameters.Add(p => p.NoResultsTemplate, "<span id='nrs-tpl'>nope</span>");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("zzz");

        Assert.IsNotNull(component.Find(".bit-srb-nrs #nrs-tpl"));
        Assert.AreEqual("true", component.Find(".bit-srb-nrs").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSearchBoxShowSuggestItemsOnFocusShouldOpenTheCalloutWithoutTyping()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ShowSuggestItemsOnFocus, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 0);
            parameters.Add(p => p.MaxSuggestCount, 0);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);

        component.Find(".bit-srb-inp").FocusIn();

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        Assert.AreEqual(Fruits.Count, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxModelessShouldCloseTheCalloutOnFocusOut()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Modeless, true);
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        component.Find(".bit-srb-inp").FocusOut();

        WaitForClosedCallout(component);
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

    [TestMethod]
    public void BitSearchBoxSuggestItemsReplacedWhileTheCalloutIsOpenShouldBeReFiltered()
    {
        var items = new List<string> { "Apple", "Banana" };

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, items);
        });

        FocusAndType(component, "a");

        Assert.AreEqual(2, component.FindAll(".bit-srb-itm").Count);

        // The kind of list a parent hands over once it has finished loading it.
        component.Render(parameters => parameters.Add(p => p.SuggestItems, new List<string> { "Apple", "Banana", "Avocado" }));

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 3);

        Assert.AreEqual("Avocado", component.FindAll(".bit-srb-itm")[2].TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxRootShouldRiseAboveTheOverlayOnlyWhileTheCalloutIsOpen()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-opn"));

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb").ClassList.Contains("bit-srb-opn"));

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForState(() => component.Find(".bit-srb").ClassList.Contains("bit-srb-opn") is false);
    }

    [TestMethod]
    public async Task BitSearchBoxReadOnlySuggestItemsShouldSayThatTheyCannotBeChosen()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.DefaultValue, "apple");
        });

        await component.InvokeAsync(() => component.Instance.ShowSuggestItems());
        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 4);

        // Read-only blocks picking a suggestion but not browsing the list, so the rows stay in the
        // accessibility tree and say that they cannot be chosen rather than swallowing the attempt.
        Assert.IsTrue(component.FindAll(".bit-srb-itm").All(i => i.GetAttribute("aria-disabled") == "true"));

        // Both the root and the callout carry it, because the callout is not a child of the root.
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-rol"));
        Assert.IsTrue(component.Find(".bit-srb-cal").ClassList.Contains("bit-srb-rol"));
    }

    [TestMethod]
    public void BitSearchBoxEditableSuggestItemsShouldNotBeMarkedDisabled()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");
        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 4);

        Assert.IsTrue(component.FindAll(".bit-srb-itm").All(i => i.HasAttribute("aria-disabled") is false));
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-rol"));
    }

    [TestMethod]
    public void BitSearchBoxTakingTheSuggestSourceAwayShouldDismissAnOpenCallout()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");
        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        component.Render(parameters => parameters.Add(p => p.SuggestItems, (IEnumerable<string>?)null));

        // The callout leaves the markup on its own, but the full screen overlay it opened with is
        // dismissed by nothing unless the component closes itself.
        component.WaitForState(() => component.FindAll(".bit-srb-cal").Count == 0);
        Assert.AreEqual(0, component.FindAll(".bit-srb-ovl").Count);
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-opn"));
    }

    [TestMethod]
    public void BitSearchBoxCalloutShouldCarryTheColorRoleOfTheComponent()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.Color, BitColor.Success);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");
        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 4);

        // The callout is a sibling of the root element, so it never inherits the role tokens and has to
        // be given the class itself - which is what paints the ring of the highlighted row and the spinner.
        var callout = component.Find(".bit-srb-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-srb-suc"));
        Assert.IsTrue(callout.ClassList.Contains("bit-srb-md"));
    }

    [TestMethod]
    public void BitSearchBoxFailingSuggestItemsProviderShouldBeReportedInsteadOfLookingEmpty()
    {
        Exception? logged = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.NoResultsText, "No matching item found.");
            parameters.Add(p => p.SuggestFailedText, "Suggestions are unavailable.");
            parameters.Add(p => p.OnSuggestFailed, ex => logged = ex);
            parameters.Add<BitSearchBoxSuggestItemsProvider>(p => p.SuggestItemsProvider,
                _ => throw new InvalidOperationException("boom"));
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.FindAll(".bit-srb-fld").Count == 1);

        // A search that could not run must not be told as a search that found nothing.
        Assert.AreEqual("Suggestions are unavailable.", component.Find(".bit-srb-fld").TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-srb-nrs").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);

        // The exception is the one failure an app would otherwise never get to see.
        Assert.IsInstanceOfType<InvalidOperationException>(logged);
        Assert.AreEqual("boom", logged!.Message);
    }

    [TestMethod]
    public void BitSearchBoxFailingSuggestItemsProviderShouldBeAnnounced()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestFailedText, "Suggestions are unavailable.");
            parameters.Add<BitSearchBoxSuggestItemsProvider>(p => p.SuggestItemsProvider,
                _ => throw new InvalidOperationException("boom"));
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb-lvr").TextContent.Contains("unavailable"));
    }

    [TestMethod]
    public void BitSearchBoxRecoveringSuggestItemsProviderShouldClearTheFailure()
    {
        var shouldFail = true;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestFailedText, "Suggestions are unavailable.");
            parameters.Add<BitSearchBoxSuggestItemsProvider>(p => p.SuggestItemsProvider, _ =>
            {
                if (shouldFail) throw new InvalidOperationException("boom");

                return ValueTask.FromResult<IEnumerable<string>>(["Apple"]);
            });
        });

        FocusAndType(component, "apple");
        component.WaitForState(() => component.FindAll(".bit-srb-fld").Count == 1);

        shouldFail = false;
        component.Find(".bit-srb-inp").Input("apples");

        // The next search starts from a clean slate, so a provider that recovers is not left reported as broken.
        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count == 1);
        Assert.AreEqual(0, component.FindAll(".bit-srb-fld").Count);
    }

    [TestMethod]
    public void BitSearchBoxWithoutFailureContentShouldNotKeepTheCalloutOpenOnAFailure()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add<BitSearchBoxSuggestItemsProvider>(p => p.SuggestItemsProvider,
                _ => throw new InvalidOperationException("boom"));
        });

        FocusAndType(component, "apple");

        // Nothing was given to report it with, so the callout closes the way an empty result does.
        WaitForClosedCallout(component);
        Assert.AreEqual(0, component.FindAll(".bit-srb-fld").Count);
    }

    #endregion



    #region diacritics & highlight

    [TestMethod]
    public void BitSearchBoxSuggestIgnoreDiacriticsShouldMatchTheFoldedText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestIgnoreDiacritics, true);
            parameters.Add(p => p.SuggestItems, new List<string> { "José", "Jürgen", "Bob" });
        });

        component.Find(".bit-srb-inp").Input("jo");

        var items = component.FindAll(".bit-srb-itm");

        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("José", items[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxWithoutIgnoreDiacriticsShouldNotMatchTheFoldedText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, new List<string> { "José", "Bob" });
        });

        component.Find(".bit-srb-inp").Input("jose");

        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxIgnoreDiacriticsShouldHighlightTheOriginalAccentedText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestIgnoreDiacritics, true);
            parameters.Add(p => p.HighlightSuggestItems, true);
            parameters.Add(p => p.SuggestItems, new List<string> { "José Álvarez" });
        });

        component.Find(".bit-srb-inp").Input("jose");

        var mark = component.Find(".bit-srb-itm mark");

        // The match is found in the folded copy but always cut out of the original text, so the
        // accent the user did not type is still the one that ends up on the screen.
        Assert.AreEqual("José", mark.TextContent);
        Assert.AreEqual("José Álvarez", component.Find(".bit-srb-itm").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxHighlightShouldFallBackToTheWordsOfAMultiWordTerm()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.HighlightSuggestItems, true);
            parameters.Add(p => p.SuggestItems, new List<string> { "Green Apple Pie" });
            // The kind of matching a SuggestItemsProvider does on the server: every word has to be
            // in the item, in any order, so the term never appears in it as a whole.
            parameters.Add(p => p.SuggestFilterFunction, (string? term, string? item) =>
                term is null || item is null ||
                term.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .All(w => item.Contains(w, StringComparison.OrdinalIgnoreCase)));
        });

        component.Find(".bit-srb-inp").Input("pie green");

        var marks = component.FindAll(".bit-srb-itm mark");

        Assert.AreEqual(2, marks.Count);
        Assert.AreEqual("Green", marks[0].TextContent);
        Assert.AreEqual("Pie", marks[1].TextContent);
    }

    [TestMethod]
    public void BitSearchBoxHighlightShouldStillPreferTheWholeTermWhenItIsThere()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.HighlightSuggestItems, true);
            parameters.Add(p => p.SuggestItems, new List<string> { "Green Apple" });
        });

        component.Find(".bit-srb-inp").Input("green apple");

        var marks = component.FindAll(".bit-srb-itm mark");

        Assert.AreEqual(1, marks.Count);
        Assert.AreEqual("Green Apple", marks[0].TextContent);
    }

    #endregion



    #region navigation & focus options

    [TestMethod]
    public void BitSearchBoxNoWrapNavigationShouldStopTheHighlightAtBothEnds()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.NoWrapNavigation, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        var input = component.Find(".bit-srb-inp");
        input.Input("apple");

        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        WaitForSelectedItem(component, 0);

        // Would have wrapped around to the last item without NoWrapNavigation.
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        WaitForSelectedItem(component, 0);

        for (var i = 0; i < 5; i++)
        {
            input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        }

        WaitForSelectedItem(component, 3);
    }

    [TestMethod]
    public void BitSearchBoxNoWrapNavigationShouldStillOpenTheListOnTheLastItemWithTheUpArrow()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.NoWrapNavigation, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        var input = component.Find(".bit-srb-inp");
        input.Input("apple");

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        WaitForSelectedItem(component, 3);
    }

    [TestMethod]
    public void BitSearchBoxSelectTextOnFocusShouldSelectTheTypedTerm()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.SelectTextOnFocus, true);
            parameters.Add(p => p.DefaultValue, "Apple");
        });

        component.Find(".bit-srb-inp").FocusIn();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.selectText"].Count);
    }

    [TestMethod]
    public void BitSearchBoxSelectTextOnFocusShouldDoNothingForAnEmptyField()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.SelectTextOnFocus, true);
        });

        component.Find(".bit-srb-inp").FocusIn();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.selectText"].Count);
    }

    [TestMethod]
    public void BitSearchBoxWithoutSelectTextOnFocusShouldNotSelectTheTypedTerm()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "Apple");
        });

        component.Find(".bit-srb-inp").FocusIn();

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.selectText"].Count);
    }

    #endregion



    #region inline auto completion

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldCompleteTheTypedTermInPlace()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count > 0);

        var invocation = Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Single();

        // The rest of the first match is appended to what was typed and left selected, so the next
        // keystroke replaces it rather than landing behind it. The typed part keeps the casing it was
        // typed with: re-casing it under the caret is what makes a completion feel like it is fighting back.
        Assert.AreEqual("apple", invocation.Arguments[1]);
        Assert.AreEqual(2, invocation.Arguments[2]);
    }

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldKeepTheTypedTermUncommitted()
    {
        string? boundValue = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, boundValue, v => boundValue = v);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count > 0);

        // Only the input element is written to: the completion is accepted by enter, by the search
        // button or by tabbing out, and until then the value still follows what the user typed.
        Assert.AreEqual("ap", boundValue);
    }

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldOnlyCompleteAnItemThatStartsWithTheTerm()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        // "Apple" matches "pple" the way every other suggestion is matched, but completing it would
        // mean rewriting what the user already typed instead of adding to it.
        FocusAndType(component, "pple");

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count > 0);

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count);
    }

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldNotPutBackWhatBackspaceTakesAway()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count == 1);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Backspace" });
        component.Find(".bit-srb-inp").Input("a");

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count);

        // ... while the very next character typed is completed again.
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "p" });
        component.Find(".bit-srb-inp").Input("ap");

        component.WaitForState(() => Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count == 2);
    }

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldNotPutBackTextThatWasCutOut()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "appl");

        component.WaitForState(() => Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count == 1);

        // A cut or a drag out of the field takes text away without a key ever being pressed, and
        // completing the shorter term again would simply put it back.
        component.Find(".bit-srb-inp").Input("ap");

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count);
    }

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldNotCompletePastTheMaxLength()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.MaxLength, 3);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count > 0);

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count);
    }

    [TestMethod]
    public void BitSearchBoxAutoFillSuggestItemShouldNotRunWhileTheFieldIsNotBeingTypedIn()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        // A value that arrives from the outside must not rewrite a field nobody is typing into.
        component.Render(parameters => parameters.Add(p => p.Value, "ap"));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count);
    }

    [TestMethod]
    public void BitSearchBoxEscapeShouldTakeBackTheInlineCompletion()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.AutoFillSuggestItem, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");
        component.WaitForState(() => Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count == 1);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // Dismissing the list gives the typed term back rather than leaving the completion behind.
        component.WaitForAssertion(() =>
            Assert.AreEqual("ap", Context.JSInterop.Invocations["BitBlazorUI.Utils.setProperty"].Last().Arguments[2]));
    }

    [TestMethod]
    public void BitSearchBoxWithoutAutoFillSuggestItemShouldNotCompleteAnything()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count > 0);

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.SearchBox.fillAndSelect"].Count);
    }

    [TestMethod]
    public void BitSearchBoxInlineAutoCompletionShouldBeAdvertisedToAssistiveTechnologies()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        Assert.AreEqual("list", component.Find(".bit-srb-inp").GetAttribute("aria-autocomplete"));

        component.Render(parameters => parameters.Add(p => p.AutoFillSuggestItem, true));

        Assert.AreEqual("both", component.Find(".bit-srb-inp").GetAttribute("aria-autocomplete"));
    }

    [TestMethod]
    public void BitSearchBoxNullSuggestItemsShouldBeDroppedInsteadOfBeingRendered()
    {
        var items = new List<string> { "Apple", null!, "Apricot" };

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, items);
            // A filter function of the app's own is free not to guard against a null item.
            parameters.Add(p => p.SuggestFilterFunction, (string? term, string? item) => true);
        });

        FocusAndType(component, "ap");

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count > 0);

        Assert.AreEqual(2, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxShouldTakeTheSuggestPoliciesFromTheCascade()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitSearchBoxParams
            {
                Immediate = true,
                AutoFillSuggestItem = true,
                MinSuggestTriggerChars = 1,
                SuggestFilterFunction = (term, item) => item?.StartsWith("B", StringComparison.OrdinalIgnoreCase) is true,
                AnnouncementProvider = args => $"{args.SuggestItems.Count} found"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSearchBox>(0);
                builder.AddAttribute(1, nameof(BitSearchBox.SuggestItems), Fruits);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("both", component.Find(".bit-srb-inp").GetAttribute("aria-autocomplete"));

        component.Find(".bit-srb-inp").FocusIn();
        component.Find(".bit-srb-inp").Input("ap");

        component.WaitForState(() => component.FindAll(".bit-srb-itm").Count > 0);

        // The matching and the announcing of every search box under the cascade are its own policies.
        Assert.AreEqual(2, component.FindAll(".bit-srb-itm").Count);
        Assert.AreEqual("2 found", component.Find(".bit-srb-lvr").TextContent.Replace("​", string.Empty).Trim());
    }

    #endregion



    #region min chars hint

    [TestMethod]
    public void BitSearchBoxMinSuggestTriggerCharsTextShouldOpenTheCalloutWithTheRemainingCount()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 3);
            parameters.Add(p => p.MinSuggestTriggerCharsText, "Type {0} more");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "a");

        component.WaitForState(() => component.FindAll(".bit-srb-hnt").Count == 1);

        Assert.AreEqual("Type 2 more", component.Find(".bit-srb-hnt").TextContent.Trim());
        Assert.AreEqual("true", component.Find(".bit-srb-inp").GetAttribute("aria-expanded"));
        Assert.AreEqual(0, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxMinSuggestTriggerCharsTextShouldDisappearOnceTheTermIsLongEnough()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 3);
            parameters.Add(p => p.MinSuggestTriggerCharsText, "Type {0} more");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        Assert.AreEqual("Type 1 more", component.Find(".bit-srb-hnt").TextContent.Trim());

        component.Find(".bit-srb-inp").Input("app");

        component.WaitForState(() => component.FindAll(".bit-srb-hnt").Count == 0);

        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);
    }

    [TestMethod]
    public void BitSearchBoxMinSuggestTriggerCharsTextShouldStayHiddenForAnEmptyField()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 3);
            parameters.Add(p => p.MinSuggestTriggerCharsText, "Type {0} more");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, string.Empty);

        Assert.AreEqual(0, component.FindAll(".bit-srb-hnt").Count);
        Assert.AreEqual("false", component.Find(".bit-srb-inp").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitSearchBoxMinSuggestTriggerCharsTextShouldReplaceTheBuiltInAnnouncement()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 3);
            parameters.Add(p => p.MinSuggestTriggerCharsText, "Type {0} more");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "a");

        component.WaitForState(() => GetAnnouncement(component) == "Type 2 more");
    }

    [TestMethod]
    public void BitSearchBoxMinSuggestTriggerCharsTextShouldBeIgnoredWithoutAMinimum()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 0);
            parameters.Add(p => p.MinSuggestTriggerCharsText, "Type {0} more");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "a");

        Assert.AreEqual(0, component.FindAll(".bit-srb-hnt").Count);
    }

    #endregion



    #region announcements

    [TestMethod]
    public void BitSearchBoxWithoutSuggestSourceShouldNotRenderTheLiveRegion()
    {
        var component = RenderComponent<BitSearchBox>();

        Assert.AreEqual(0, component.FindAll(".bit-srb-lvr").Count);
    }

    [TestMethod]
    public void BitSearchBoxLiveRegionShouldBeAPoliteAtomicStatus()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SuggestItems, Fruits));

        var liveRegion = component.Find(".bit-srb-lvr");

        Assert.AreEqual("status", liveRegion.GetAttribute("role"));
        Assert.AreEqual("polite", liveRegion.GetAttribute("aria-live"));
        Assert.AreEqual("true", liveRegion.GetAttribute("aria-atomic"));
        // it must start empty, otherwise the very first announcement is swallowed.
        Assert.AreEqual(string.Empty, GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxShouldAnnounceTheNumberOfSuggestItems()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");

        StringAssert.StartsWith(GetAnnouncement(component), "4 suggestions available.");

        component.Find(".bit-srb-inp").Input("banana");

        StringAssert.StartsWith(GetAnnouncement(component), "1 suggestion available.");
    }

    [TestMethod]
    public void BitSearchBoxShouldAnnounceThatNothingWasFound()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "zzz");

        Assert.AreEqual("No suggestion found.", GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxShouldAnnounceTheNoResultsTextWhenOneIsGiven()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.NoResultsText, "Nothing here");
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "zzz");

        Assert.AreEqual("Nothing here", GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxShouldAnnounceHowManyCharactersAreStillNeeded()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 3);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "ap");

        Assert.AreEqual("Type 3 or more characters for suggestions.", GetAnnouncement(component));

        // an empty field is the starting state rather than a failed search, so it says nothing.
        component.Find(".bit-srb-inp").Input(string.Empty);

        Assert.AreEqual(string.Empty, GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxShouldMakeARepeatedAnnouncementUniqueSoItGetsReadAgain()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "zzz");
        var first = component.Find(".bit-srb-lvr").TextContent;

        component.Find(".bit-srb-inp").Input("yyy");
        var second = component.Find(".bit-srb-lvr").TextContent;

        // the wording is identical, so the raw text has to differ for a screen reader to read it twice.
        Assert.AreEqual("No suggestion found.", GetAnnouncement(component));
        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    public void BitSearchBoxAnnouncementProviderShouldReplaceTheBuiltInTexts()
    {
        BitSearchBoxAnnouncementArgs? received = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.AnnouncementProvider, args =>
            {
                received = args;
                return $"{args.SuggestItems.Count} nataye baraye {args.SearchTerm}";
            });
        });

        FocusAndType(component, "apple");

        Assert.AreEqual("4 nataye baraye apple", GetAnnouncement(component));
        Assert.IsNotNull(received);
        Assert.AreEqual("apple", received.SearchTerm);
        Assert.AreEqual(4, received.SuggestItems.Count);
        Assert.IsFalse(received.IsLoading);
        Assert.IsFalse(received.IsSearchTermTooShort);
        Assert.AreEqual(1, received.MinSuggestTriggerChars);
    }

    [TestMethod]
    public void BitSearchBoxAnnouncementProviderShouldBeAbleToStaySilent()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.AnnouncementProvider, _ => null);
        });

        FocusAndType(component, "apple");

        Assert.AreEqual(string.Empty, GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxAnnouncementProviderShouldSeeTheTooShortTerm()
    {
        BitSearchBoxAnnouncementArgs? received = null;

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 4);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.AnnouncementProvider, args => { received = args; return "x"; });
        });

        FocusAndType(component, "ap");

        Assert.IsNotNull(received);
        Assert.IsTrue(received.IsSearchTermTooShort);
        Assert.AreEqual(0, received.SuggestItems.Count);
        Assert.AreEqual(4, received.MinSuggestTriggerChars);
    }

    [TestMethod]
    public void BitSearchBoxShouldAnnounceAFruitlessSearchEvenThoughTheCalloutNeverOpens()
    {
        // without a NoResultsText there is nothing to show, so the callout stays closed and the live
        // region is the only thing that tells a screen reader user that the search returned nothing.
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");

        component.Find(".bit-srb-inp").Input("zzz");

        WaitForClosedCallout(component);
        Assert.AreEqual("No suggestion found.", GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxShouldKeepTheLastAnnouncementWhenTheCalloutIsDismissed()
    {
        // a live region only speaks when its text changes, so a dismissed callout can leave its last
        // message behind; wiping it would cost the announcements of the searches that close the callout.
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");
        var announcement = GetAnnouncement(component);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        WaitForClosedCallout(component);
        Assert.AreEqual(announcement, GetAnnouncement(component));
    }

    [TestMethod]
    public void BitSearchBoxShouldStaySilentWhileTheInputIsNotFocused()
    {
        // a suggest list nobody can see (the callout does not open when the field is not focused)
        // must not be announced either.
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        component.Find(".bit-srb-inp").Input("apple");

        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);
        Assert.AreEqual(string.Empty, GetAnnouncement(component));
    }

    #endregion



    #region public api

    [TestMethod]
    public async Task BitSearchBoxShowAndHideSuggestItemsShouldDriveTheCallout()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "apple");
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        Assert.IsFalse(component.Instance.IsSuggestItemsOpen);
        Assert.AreEqual("false", component.Find(".bit-srb-inp").GetAttribute("aria-expanded"));

        // it forces the callout open even though the input never got the focus.
        await component.InvokeAsync(() => component.Instance.ShowSuggestItems());

        component.WaitForState(() => component.Find(".bit-srb-inp").GetAttribute("aria-expanded") == "true");
        Assert.IsTrue(component.Instance.IsSuggestItemsOpen);
        Assert.AreEqual(4, component.FindAll(".bit-srb-itm").Count);

        await component.InvokeAsync(() => component.Instance.HideSuggestItems());

        WaitForClosedCallout(component);
        Assert.IsFalse(component.Instance.IsSuggestItemsOpen);
    }

    [TestMethod]
    public async Task BitSearchBoxShowSuggestItemsShouldAnnounceTheResult()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Value, "apple");
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        await component.InvokeAsync(() => component.Instance.ShowSuggestItems());

        StringAssert.StartsWith(GetAnnouncement(component), "4 suggestions available.");
    }

    [TestMethod]
    public async Task BitSearchBoxDisabledShouldIgnoreThePublicMethods()
    {
        string? value = "apple";

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        await component.InvokeAsync(() => component.Instance.Clear());
        await component.InvokeAsync(() => component.Instance.ShowSuggestItems());

        Assert.AreEqual("apple", value);
        Assert.IsFalse(component.Instance.IsSuggestItemsOpen);
    }

    [TestMethod]
    public void BitSearchBoxOnSuggestItemsToggleShouldReportBothTransitions()
    {
        var toggles = new List<bool>();

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.OnSuggestItemsToggle, v => toggles.Add(v));
        });

        FocusAndType(component, "apple");

        component.WaitForState(() => toggles.Count == 1);
        Assert.IsTrue(toggles[0]);

        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        component.WaitForState(() => toggles.Count == 2);
        Assert.IsFalse(toggles[1]);
    }

    [TestMethod]
    public void BitSearchBoxOnSuggestItemsToggleShouldNotFireForAnAlreadyClosedCallout()
    {
        var toggles = new List<bool>();

        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Immediate, true);
            parameters.Add(p => p.MinSuggestTriggerChars, 1);
            parameters.Add(p => p.SuggestItems, Fruits);
            parameters.Add(p => p.OnSuggestItemsToggle, v => toggles.Add(v));
        });

        // no suggestion matches, so the callout never opens and there is nothing to report.
        component.Find(".bit-srb-inp").Input("zzz");
        component.Find(".bit-srb-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, toggles.Count);
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

        Assert.AreEqual(isValid ? 1 : 0, component.Instance.ValidCount);
        Assert.AreEqual(isValid ? 0 : 1, component.Instance.InvalidCount);

        var input = component.Find("input.bit-srb-inp");
        if (isValid)
        {
            // a valid initial value is replaced by an invalid one, and the other way around,
            // so that the second submit always lands on the opposite outcome of the first one.
            input.Input("bit.com");
        }
        else
        {
            input.Input("test@bit.com");
        }

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
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

    #region description, error message & cascading parameters

    [TestMethod]
    public void BitSearchBoxDescriptionShouldRenderAndDescribeTheInput()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Description, "Searches products by name.");
        });

        var description = component.Find(".bit-srb-des");
        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual("Searches products by name.", description.TextContent.Trim());
        Assert.AreEqual(description.GetAttribute("id"), input.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxDescriptionTemplateShouldReplaceTheDescriptionText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Description, "plain");
            parameters.Add(p => p.DescriptionTemplate, "<span id='custom-description'>custom</span>");
        });

        var description = component.Find(".bit-srb-des");

        Assert.IsNotNull(component.Find("#custom-description"));
        Assert.IsFalse(description.TextContent.Contains("plain"));
        Assert.AreEqual(description.GetAttribute("id"), component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxAriaDescriptionShouldRenderHiddenAndDescribeTheInput()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, "Results update as you type.");
        });

        var ariaDescription = component.Find(".bit-srb-ard");

        Assert.AreEqual("Results update as you type.", ariaDescription.TextContent.Trim());
        Assert.AreEqual(ariaDescription.GetAttribute("id"), component.Find(".bit-srb-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxWithoutDescriptionsShouldNotRenderDescribedBy()
    {
        var component = RenderComponent<BitSearchBox>();

        Assert.AreEqual(0, component.FindAll(".bit-srb-des").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-ard").Count);
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitSearchBoxDescribedByShouldKeepTheConsumerOwnValueFirst()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { { "aria-describedby", "outside-hint" } });
            parameters.Add(p => p.ErrorMessage, "Rejected.");
            parameters.Add(p => p.Description, "A hint.");
            parameters.Add(p => p.AriaDescription, "For screen readers.");
        });

        var describedBy = component.Find(".bit-srb-inp").GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(4, describedBy.Length);
        Assert.AreEqual("outside-hint", describedBy[0]);
        Assert.AreEqual(component.Find(".bit-srb-erm").GetAttribute("id"), describedBy[1]);
        Assert.AreEqual(component.Find(".bit-srb-des").GetAttribute("id"), describedBy[2]);
        Assert.AreEqual(component.Find(".bit-srb-ard").GetAttribute("id"), describedBy[3]);
    }

    [TestMethod]
    public void BitSearchBoxErrorMessageShouldMarkTheFieldInvalidAndBeAnnounced()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, "The term is too short.");
        });

        var input = component.Find(".bit-srb-inp");

        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
        Assert.AreEqual("The term is too short.", component.Find(".bit-srb-erm").TextContent.Trim());
        Assert.AreEqual(component.Find(".bit-srb-erm").GetAttribute("id"), input.GetAttribute("aria-describedby"));
        Assert.AreEqual("The term is too short.", component.Find(".bit-srb-elv").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxErrorLiveRegionShouldAlwaysBeRendered()
    {
        var component = RenderComponent<BitSearchBox>();

        var liveRegion = component.Find(".bit-srb-elv");

        Assert.AreEqual("status", liveRegion.GetAttribute("role"));
        Assert.AreEqual(string.Empty, liveRegion.TextContent);
    }

    [TestMethod]
    public void BitSearchBoxErrorMessageTemplateShouldReplaceTheErrorText()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ErrorMessageTemplate, "<span id='custom-error'>custom</span>");
        });

        Assert.IsNotNull(component.Find("#custom-error"));
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-inv"));

        // Only the plain text is announced, since a template is free to render anything at all.
        Assert.AreEqual(string.Empty, component.Find(".bit-srb-elv").TextContent);
    }

    [TestMethod]
    public void BitSearchBoxWithoutErrorMessageShouldNotBeInvalid()
    {
        var component = RenderComponent<BitSearchBox>();

        Assert.AreEqual(0, component.FindAll(".bit-srb-erm").Count);
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-inv"));
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitSearchBoxAriaLabelShouldWinOverTheLabelElement()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Label, "Search products");
            parameters.Add(p => p.AriaLabel, "Search the whole catalog");
        });

        var input = component.Find(".bit-srb-inp");

        // aria-labelledby takes precedence over aria-label, so pointing at the visible label while a
        // name of its own was given would quietly throw that name away.
        Assert.AreEqual("Search the whole catalog", input.GetAttribute("aria-label"));
        Assert.IsFalse(input.HasAttribute("aria-labelledby"));
        Assert.AreEqual(input.GetAttribute("id"), component.Find("label.bit-srb-lbl").GetAttribute("for"));
    }

    [TestMethod]
    public void BitSearchBoxShouldRenderTheAutoCapitalizeAndAutoCorrectAttributes()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.AutoCapitalize, "none");
            parameters.Add(p => p.AutoCorrect, false);
        });

        var input = component.Find(".bit-srb-inp");

        Assert.AreEqual("none", input.GetAttribute("autocapitalize"));
        Assert.AreEqual("off", input.GetAttribute("autocorrect"));

        var without = RenderComponent<BitSearchBox>();

        Assert.IsFalse(without.Find(".bit-srb-inp").HasAttribute("autocapitalize"));
        Assert.IsFalse(without.Find(".bit-srb-inp").HasAttribute("autocorrect"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-srb-sm"),
        DataRow(BitSize.Medium, "bit-srb-md"),
        DataRow(BitSize.Large, "bit-srb-lg")
    ]
    public void BitSearchBoxCalloutShouldCarryTheSizeClass(BitSize size, string expectedClass)
    {
        // The callout is rendered as a sibling of the root element, so the size class of the
        // component never reaches it through the cascade and it has to be given one of its own.
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Size, size);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        Assert.IsTrue(component.Find(".bit-srb-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitSearchBoxParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSearchBox)}", BitSearchBoxParams.ParamName);
    }

    [TestMethod]
    public void BitSearchBoxParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSearchBoxParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitSearchBoxParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSearchBoxShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitSearchBoxParams
            {
                Size = BitSize.Large,
                Color = BitColor.Success,
                Underlined = true,
                FullWidth = true,
                NoBorder = true,
                Placeholder = "Cascaded placeholder",
                ClearButtonAriaLabel = "Cascaded clear",
                MaxLength = 12
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSearchBox>(0);
                builder.CloseComponent();
            });
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
        var paramsList = new List<IBitComponentParams>
        {
            new BitSearchBoxParams
            {
                Size = BitSize.Large,
                Color = BitColor.Success,
                Placeholder = "Cascaded placeholder"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSearchBox>(0);
                builder.AddAttribute(1, nameof(BitSearchBox.Size), BitSize.Small);
                builder.AddAttribute(2, nameof(BitSearchBox.Placeholder), "Own placeholder");
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-srb");

        Assert.IsTrue(root.ClassList.Contains("bit-srb-sm"));
        Assert.AreEqual("Own placeholder", component.Find(".bit-srb-inp").GetAttribute("placeholder"));

        // What the search box left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-srb-suc"));
    }

    [TestMethod]
    public void BitSearchBoxRequiredShouldRenderAriaRequired()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.SuggestItems, Fruits);
        });

        // The combobox role replaces the implicit one of the input, so the required state is stated in
        // the ARIA property rather than left to the mapping of the native attribute.
        Assert.AreEqual("true", component.Find(".bit-srb-inp").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitSearchBoxOnlyTheDefaultSearchGlyphShouldBeMirroredInRtl()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ShowSearchButton, true);
        });

        // The default glyph is a directional arrow, which an RTL layout has to turn around.
        Assert.IsTrue(component.Find(".bit-srb-sbn i").ClassList.Contains("bit-srb-dsi"));

        component.Render(parameters => parameters.Add(p => p.SearchButtonIconName, "Search"));

        // A magnifier means the same thing both ways, and mirroring it would only render it backwards.
        Assert.IsFalse(component.Find(".bit-srb-sbn i").ClassList.Contains("bit-srb-dsi"));
    }

    #endregion

    #region loading & search button text

    [TestMethod]
    public void BitSearchBoxLoadingShouldShowTheSpinnerInPlaceOfTheClearButton()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "apple");
            parameters.Add(p => p.Loading, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-srb-lsp .bit-srb-spn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-srb-cbt").Count);
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-lng"));
        Assert.AreEqual("true", component.Find(".bit-srb-inp").GetAttribute("aria-busy"));
        Assert.AreEqual("Searching", component.Find(".bit-srb-elv").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxWithoutLoadingShouldKeepTheClearButton()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.DefaultValue, "apple"));

        Assert.AreEqual(0, component.FindAll(".bit-srb-lsp").Count);
        Assert.AreEqual(1, component.FindAll(".bit-srb-cbt").Count);
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public void BitSearchBoxLoadingAriaLabelShouldBeAnnounced()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.LoadingAriaLabel, "Looking for matches");
        });

        Assert.AreEqual("Looking for matches", component.Find(".bit-srb-elv").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxErrorMessageShouldWinOverTheLoadingAnnouncement()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.Loading, true);
            parameters.Add(p => p.ErrorMessage, "The search failed.");
        });

        Assert.AreEqual("The search failed.", component.Find(".bit-srb-elv").TextContent.Trim());
    }

    [TestMethod]
    public void BitSearchBoxSearchButtonTextShouldRenderAndNameTheButton()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.SearchButtonText, "Search");
            parameters.Add(p => p.SearchButtonAriaLabel, "Run the search");
        });

        var button = component.Find(".bit-srb-sbn");

        Assert.AreEqual("Search", component.Find(".bit-srb-sbx").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-sbt"));

        // The visible label already names the button, so an aria-label on top of it would replace what is
        // written on it with a second, unrelated string, and a tooltip repeating it is pure noise.
        Assert.IsFalse(button.HasAttribute("aria-label"));
        Assert.IsFalse(button.HasAttribute("title"));
    }

    [TestMethod]
    public void BitSearchBoxSearchButtonWithoutTextShouldKeepItsAriaLabel()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.SearchButtonAriaLabel, "Run the search");
        });

        Assert.AreEqual(0, component.FindAll(".bit-srb-sbx").Count);
        Assert.IsFalse(component.Find(".bit-srb").ClassList.Contains("bit-srb-sbt"));
        Assert.AreEqual("Run the search", component.Find(".bit-srb-sbn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSearchBoxSearchButtonTemplateShouldReplaceTheTextAndTheIcon()
    {
        var component = RenderComponent<BitSearchBox>(parameters =>
        {
            parameters.Add(p => p.ShowSearchButton, true);
            parameters.Add(p => p.SearchButtonText, "Search");
            parameters.Add(p => p.SearchButtonTemplate, "<span id='custom-search'>go</span>");
        });

        Assert.IsNotNull(component.Find("#custom-search"));
        Assert.AreEqual(0, component.FindAll(".bit-srb-sbx").Count);
    }

    [TestMethod]
    public void BitSearchBoxOverlayShouldBeHiddenFromAssistiveTechnologies()
    {
        var component = RenderComponent<BitSearchBox>(parameters => parameters.Add(p => p.SuggestItems, Fruits));

        Assert.AreEqual("true", component.Find(".bit-srb-ovl").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSearchBoxShouldTakeInheritedParametersFromTheCascade()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitSearchBoxParams { ReadOnly = true, Required = true, MaxLength = 8 }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSearchBox>(0);
                builder.CloseComponent();
            });
        });

        // ReadOnly and Required are declared by the input base classes rather than by the search box,
        // so they are the ones the generated HasNotBeenSet knows nothing about.
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("readonly"));
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("required"));
        Assert.IsTrue(component.Find(".bit-srb").ClassList.Contains("bit-srb-req"));
        Assert.AreEqual("8", component.Find(".bit-srb-inp").GetAttribute("maxlength"));
    }

    [TestMethod]
    public void BitSearchBoxOwnInheritedParametersShouldWinOverTheCascade()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitSearchBoxParams { ReadOnly = true, Required = true, Immediate = true }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSearchBox>(0);
                builder.AddAttribute(1, nameof(BitSearchBox.ReadOnly), false);
                builder.CloseComponent();
            });
        });

        // The search box wrote ReadOnly="false" for itself, so the cascade must not turn it back on,
        // while Required, which it left unset, is still filled in from there.
        Assert.IsFalse(component.Find(".bit-srb-inp").HasAttribute("readonly"));
        Assert.IsTrue(component.Find(".bit-srb-inp").HasAttribute("required"));
    }

    #endregion
}
