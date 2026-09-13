using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

[TestClass]
public class BitTagsInputTests : BunitTestContext
{
    #region rendering & state

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTagsInputIsEnabledTest(bool isEnabled)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = com.Find(".bit-tgi");

        Assert.AreEqual(!isEnabled, root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(!isEnabled, com.Find(".bit-tgi-inp").HasAttribute("disabled"));
    }

    [TestMethod,
        DataRow("Add a tag"),
        DataRow("Search...")]
    public void BitTagsInputPlaceholderTest(string placeholder)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var input = com.Find(".bit-tgi-inp");

        Assert.AreEqual(placeholder, input.GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitTagsInputPlaceholderIsDroppedWhenTagsExistTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Add a tag");
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("placeholder"));
    }

    [TestMethod]
    public void BitTagsInputTagsPlaceholderTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Add a tag");
            parameters.Add(p => p.TagsPlaceholder, "add another...");
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual("add another...", com.Find(".bit-tgi-inp").GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitTagsInputPlaceholderIsUpdatedWhenParameterChangesTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "first");
        });

        Assert.AreEqual("first", com.Find(".bit-tgi-inp").GetAttribute("placeholder"));

        com.Render(parameters => parameters.Add(p => p.Placeholder, "second"));

        Assert.AreEqual("second", com.Find(".bit-tgi-inp").GetAttribute("placeholder"));
    }

    [TestMethod,
        DataRow("My label"),
        DataRow("Tags")]
    public void BitTagsInputLabelTest(string label)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var labelEl = com.Find(".bit-tgi-lbl");

        Assert.AreEqual(label, labelEl.TextContent.Trim());
        Assert.AreEqual(labelEl.Id, com.Find(".bit-tgi-inp").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitTagsInputAriaLabelIsOnlyRenderedWithoutALabelTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "tags");
        });

        Assert.AreEqual("tags", com.Find(".bit-tgi-inp").GetAttribute("aria-label"));

        com.Render(parameters => parameters.Add(p => p.Label, "Tags"));

        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTagsInputDescriptionTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Description, "Press Enter after each tag.");
        });

        var description = com.Find(".bit-tgi-dsc");

        Assert.AreEqual("Press Enter after each tag.", description.TextContent.Trim());
        Assert.AreEqual(description.Id, com.Find(".bit-tgi-inp").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTagsInputNoDescriptionMeansNoAriaDescribedByTest()
    {
        var com = RenderComponent<BitTagsInput>();

        Assert.AreEqual(0, com.FindAll(".bit-tgi-dsc").Count);
        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("aria-describedby"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTagsInputNoBorderTest(bool noBorder)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.NoBorder, noBorder);
        });

        var root = com.Find(".bit-tgi");

        Assert.AreEqual(noBorder, root.ClassList.Contains("bit-tgi-nbd"));
    }

    [TestMethod,
        DataRow(BitVariant.Fill, "bit-tgi-fil"),
        DataRow(BitVariant.Outline, "bit-tgi-otl"),
        DataRow(BitVariant.Text, "bit-tgi-txt"),
        DataRow(null, "bit-tgi-otl")]
    public void BitTagsInputVariantTest(BitVariant? variant, string expectedClass)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-tgi-sm"),
        DataRow(BitSize.Medium, "bit-tgi-md"),
        DataRow(BitSize.Large, "bit-tgi-lg"),
        DataRow(null, "bit-tgi-md")]
    public void BitTagsInputSizeTest(BitSize? size, string expectedClass)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-tgi-pri"),
        DataRow(BitColor.Success, "bit-tgi-suc"),
        DataRow(BitColor.Error, "bit-tgi-err"),
        DataRow(null, "bit-tgi-pri")]
    public void BitTagsInputColorTest(BitColor? color, string expectedClass)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitVariant.Fill, "bit-tgi-tgf"),
        DataRow(BitVariant.Outline, "bit-tgi-tgo"),
        DataRow(BitVariant.Text, "bit-tgi-tgt"),
        DataRow(null, "bit-tgi-tgf")]
    public void BitTagsInputTagVariantTest(BitVariant? tagVariant, string expectedClass)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.TagVariant, tagVariant);
        });

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitTagsInputRequiredClassOnlyWithALabelTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Required, true);
        });

        Assert.IsFalse(com.Find(".bit-tgi").ClassList.Contains("bit-tgi-req"));

        com.Render(parameters => parameters.Add(p => p.Label, "Tags"));

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains("bit-tgi-req"));
    }

    [TestMethod,
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Ltr, "ltr")]
    public void BitTagsInputDirTest(BitDir dir, string expectedDir)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        Assert.AreEqual(expectedDir, com.Find(".bit-tgi").GetAttribute("dir"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTagsInputReadOnlyTest(bool readOnly)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        var input = com.Find(".bit-tgi-inp");

        Assert.AreEqual(readOnly, input.HasAttribute("readonly"));
        Assert.AreEqual(readOnly, com.Find(".bit-tgi").ClassList.Contains("bit-tgi-rdl"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTagsInputRequiredIsAnnouncedTest(bool required)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Required, required);
        });

        // The asterisk of the label is drawn rather than announced, so the state has to be carried by
        // the input itself for a screen reader to know the field is mandatory.
        Assert.AreEqual(required ? "true" : null, com.Find(".bit-tgi-inp").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitTagsInputTurnsTheBrowserAutofillOffTest()
    {
        var com = RenderComponent<BitTagsInput>();

        // The autofill of the browser would otherwise be offered over the suggestion list of the field.
        Assert.AreEqual("off", com.Find(".bit-tgi-inp").GetAttribute("autocomplete"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTagsInputNoTrimReachesTheScriptSideTest(bool noTrim)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.NoTrim, noTrim);
        });

        // The script side decides whether the Enter key has anything to confirm, and a field that keeps
        // its whitespace has something to confirm where a trimming one has not.
        Assert.AreEqual(noTrim, com.Find(".bit-tgi-inp").HasAttribute("data-no-trim"));
    }

    [TestMethod]
    public void BitTagsInputTagTextCarriesTheWholeTagAsATooltipTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a-very-long-tag-that-does-not-fit" });
        });

        // A chip too wide for the field is cut off with an ellipsis, so the whole of it lives in the title.
        Assert.AreEqual("a-very-long-tag-that-does-not-fit", com.Find(".bit-tgi-ttx").GetAttribute("title"));
    }

    [TestMethod]
    public void BitTagsInputPrefixAndSuffixTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Prefix, "To:");
            parameters.Add(p => p.Suffix, "cm");
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual("To:", com.Find(".bit-tgi-pre").TextContent.Trim());
        Assert.AreEqual("cm", com.Find(".bit-tgi-suf").TextContent.Trim());

        // Neither of them is part of the value the field posts.
        com.Render(parameters => parameters.Add(p => p.Name, "tags"));

        Assert.AreEqual("apple", com.Find("input[type=hidden]").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTagsInputNoPrefixOrSuffixByDefaultTest()
    {
        var com = RenderComponent<BitTagsInput>();

        Assert.AreEqual(0, com.FindAll(".bit-tgi-pre").Count);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-suf").Count);
    }

    [TestMethod]
    public void BitTagsInputPrefixAndSuffixTemplatesWinOverTheTextTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Prefix, "To:");
            parameters.Add(p => p.Suffix, "cm");
            parameters.Add(p => p.PrefixTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-prefix");
                builder.AddContent(2, "prefix");
                builder.CloseElement();
            }));
            parameters.Add(p => p.SuffixTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-suffix");
                builder.AddContent(2, "suffix");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("prefix", com.Find(".bit-tgi-pre .custom-prefix").TextContent);
        Assert.AreEqual("suffix", com.Find(".bit-tgi-suf .custom-suffix").TextContent);
    }

    [TestMethod]
    public void BitTagsInputTagsCarryTheirPositionInTheWholeListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d", "e" });
        });

        var tags = com.FindAll(".bit-tgi-tag");

        // Only two of the five are drawn, so counting the elements off would say "2 of 2" over a list
        // that holds five; the position is spelled out instead.
        Assert.AreEqual(2, tags.Count);
        Assert.AreEqual("1", tags[0].GetAttribute("aria-posinset"));
        Assert.AreEqual("2", tags[1].GetAttribute("aria-posinset"));
        Assert.IsTrue(tags.All(t => t.GetAttribute("aria-setsize") == "5"));
    }

    [TestMethod]
    public void BitTagsInputInitialValueRendersTagsTest()
    {
        var initialTags = new List<string> { "apple", "banana", "cherry" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, initialTags);
        });

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.AreEqual(initialTags.Count, tags.Count);
        Assert.AreEqual("apple", tags[0].QuerySelector(".bit-tgi-ttx")!.TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputRendersAnAccessibleListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        Assert.AreEqual("list", com.Find(".bit-tgi-tgs").GetAttribute("role"));

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.IsTrue(tags.All(t => t.GetAttribute("role") == "listitem"));

        // Roving tabindex: exactly one tag owns the tab stop of the whole list.
        Assert.AreEqual(1, tags.Count(t => t.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("0", tags[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", tags[1].GetAttribute("tabindex"));

        // The dismiss buttons stay out of the tab order, the tags being the stop that replaces them.
        Assert.IsTrue(com.FindAll(".bit-tgi-dbt").All(b => b.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void BitTagsInputTagsAreNotFocusableWhenDisabledTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(com.FindAll(".bit-tgi-tag").All(t => t.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void BitTagsInputDismissButtonAccessibilityTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var button = com.Find(".bit-tgi-dbt");

        Assert.AreEqual("Remove apple", button.GetAttribute("aria-label"));
        Assert.AreEqual("Remove", button.GetAttribute("title"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.DismissAriaLabelFormat, "Delete {0} tag");
            parameters.Add(p => p.DismissTitle, "Delete");
        });

        button = com.Find(".bit-tgi-dbt");

        Assert.AreEqual("Delete apple tag", button.GetAttribute("aria-label"));
        Assert.AreEqual("Delete", button.GetAttribute("title"));
    }

    [TestMethod]
    public void BitTagsInputDismissButtonNotRenderedWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-dbt").Count);
    }

    [TestMethod]
    public void BitTagsInputDismissButtonNotRenderedWhenDisabledTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-dbt").Count);
    }

    [TestMethod]
    public void BitTagsInputTagTemplateTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.TagTemplate, (RenderFragment<string>)(tag => builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddAttribute(1, "class", "custom-tag-content");
                builder.AddContent(2, tag);
                builder.CloseElement();
            }));
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-ttx").Count);
        Assert.AreEqual("apple", com.Find(".custom-tag-content").TextContent);
    }

    [TestMethod]
    public async Task BitTagsInputClassesAndStylesTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Label, "Tags");
            parameters.Add(p => p.Description, "hint");
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.ShowCounter, true);
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.Classes, new BitTagsInputClassStyles
            {
                Root = "custom-root",
                Label = "custom-label",
                Description = "custom-description",
                InputContainer = "custom-container",
                TagsContainer = "custom-tags",
                Tag = "custom-tag",
                FocusedTag = "custom-focused-tag",
                TagText = "custom-tag-text",
                DismissButton = "custom-dismiss",
                DismissIcon = "custom-dismiss-icon",
                Input = "custom-input",
                EditInput = "custom-edit-input",
                Counter = "custom-counter",
                ClearButton = "custom-clear",
                ClearIcon = "custom-clear-icon",
            });
            parameters.Add(p => p.Styles, new BitTagsInputClassStyles
            {
                Tag = "color: red",
                FocusedTag = "background: blue",
                EditInput = "color: green",
                Counter = "color: purple",
                ClearIcon = "color: teal",
            });
        });

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains("custom-root"));
        Assert.IsTrue(com.Find(".bit-tgi-lbl").ClassList.Contains("custom-label"));
        Assert.IsTrue(com.Find(".bit-tgi-dsc").ClassList.Contains("custom-description"));
        Assert.IsTrue(com.Find(".bit-tgi-cnt").ClassList.Contains("custom-container"));
        Assert.IsTrue(com.Find(".bit-tgi-tgs").ClassList.Contains("custom-tags"));
        Assert.IsTrue(com.Find(".bit-tgi-tag").ClassList.Contains("custom-tag"));
        Assert.IsTrue(com.Find(".bit-tgi-ttx").ClassList.Contains("custom-tag-text"));
        Assert.IsTrue(com.Find(".bit-tgi-dbt").ClassList.Contains("custom-dismiss"));
        Assert.IsTrue(com.Find(".bit-tgi-dsi").ClassList.Contains("custom-dismiss-icon"));
        Assert.IsTrue(com.Find(".bit-tgi-inp").ClassList.Contains("custom-input"));
        Assert.IsTrue(com.Find(".bit-tgi-cbt").ClassList.Contains("custom-clear"));
        Assert.IsTrue(com.Find(".bit-tgi-cli").ClassList.Contains("custom-clear-icon"));
        Assert.AreEqual("color: teal", com.Find(".bit-tgi-cli").GetAttribute("style"));

        Assert.IsTrue(com.Find(".bit-tgi-cnr").ClassList.Contains("custom-counter"));
        Assert.AreEqual("color: purple", com.Find(".bit-tgi-cnr").GetAttribute("style"));

        // The tag that is not focused carries the Tag styling on its own.
        Assert.IsFalse(com.Find(".bit-tgi-tag").ClassList.Contains("custom-focused-tag"));
        Assert.AreEqual("color: red", com.Find(".bit-tgi-tag").GetAttribute("style"));

        // Walking backwards out of the empty input focuses the last tag, which is what the focused
        // styling is drawn from.
        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.IsTrue(com.Find(".bit-tgi-tag").ClassList.Contains("custom-focused-tag"));
        Assert.AreEqual("color: red;background: blue", com.Find(".bit-tgi-tag").GetAttribute("style"));

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        Assert.IsTrue(com.Find(".bit-tgi-eip").ClassList.Contains("custom-edit-input"));
        Assert.AreEqual("color: green", com.Find(".bit-tgi-eip").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTagsInputGetTagClassAndStyleDressEachTagOnItsOwnTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.Classes, new BitTagsInputClassStyles { Tag = "every-tag" });
            parameters.Add(p => p.Styles, new BitTagsInputClassStyles { Tag = "color: red" });
            parameters.Add(p => p.GetTagClass, (Func<string, string?>)(tag => tag == "apple" ? "fruit-apple" : null));
            parameters.Add(p => p.GetTagStyle, (Func<string, string?>)(tag => tag == "banana" ? "color: yellow" : null));
        });

        var tags = com.FindAll(".bit-tgi-tag");

        // The classes of the component and of the Classes are kept, the ones of this single tag added on top.
        Assert.IsTrue(tags[0].ClassList.Contains("bit-tgi-tag"));
        Assert.IsTrue(tags[0].ClassList.Contains("every-tag"));
        Assert.IsTrue(tags[0].ClassList.Contains("fruit-apple"));
        Assert.IsFalse(tags[1].ClassList.Contains("fruit-apple"));

        // The style of the single tag is appended last, so it wins over the one every tag carries.
        Assert.AreEqual("color: red", tags[0].GetAttribute("style"));
        Assert.AreEqual("color: red;color: yellow", tags[1].GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitTagsInputGetTagStyleIsAppendedAfterTheFocusedTagStyleTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.Styles, new BitTagsInputClassStyles { Tag = "color: red", FocusedTag = "background: blue" });
            parameters.Add(p => p.GetTagStyle, (Func<string, string?>)(_ => "outline: 1px solid green"));
        });

        Assert.AreEqual("color: red;outline: 1px solid green", com.Find(".bit-tgi-tag").GetAttribute("style"));

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("color: red;background: blue;outline: 1px solid green", com.Find(".bit-tgi-tag").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTagsInputThrowingTagStylingLeavesTheChipPlainTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.GetTagClass, (Func<string, string?>)(_ => throw new InvalidOperationException()));
            parameters.Add(p => p.GetTagStyle, (Func<string, string?>)(_ => throw new InvalidOperationException()));
        });

        // A function of the consumer must not be able to break the render of the whole field.
        Assert.AreEqual("bit-tgi-tag", com.Find(".bit-tgi-tag").GetAttribute("class"));
        Assert.IsNull(com.Find(".bit-tgi-tag").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTagsInputHiddenFieldPostsTheWholeListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Name, "tags");
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        var hidden = com.Find("input[type=hidden]");

        Assert.AreEqual("tags", hidden.GetAttribute("name"));
        Assert.AreEqual("apple,banana", hidden.GetAttribute("value"));

        // The visible input holds the text being typed, which must never be what the form posts.
        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("name"));
    }

    [TestMethod]
    public void BitTagsInputHiddenFieldUsesTheFirstSeparatorTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Name, "tags");
            parameters.Add(p => p.Separators, new[] { ";", "," });
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        Assert.AreEqual("apple;banana", com.Find("input[type=hidden]").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTagsInputNoHiddenFieldWithoutANameTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual(0, com.FindAll("input[type=hidden]").Count);
    }

    [TestMethod]
    public void BitTagsInputSeparatorsReachTheScriptSideTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { ",", ";" });
        });

        Assert.AreEqual("[\",\",\";\"]", com.Find(".bit-tgi-inp").GetAttribute("data-separators"));
    }

    [TestMethod]
    public void BitTagsInputLiveRegionIsRenderedTest()
    {
        var com = RenderComponent<BitTagsInput>();

        var liveRegion = com.Find(".bit-tgi-lvr");

        Assert.AreEqual("status", liveRegion.GetAttribute("role"));
        Assert.AreEqual("polite", liveRegion.GetAttribute("aria-live"));
        Assert.AreEqual("true", liveRegion.GetAttribute("aria-atomic"));
    }

    [TestMethod]
    public async Task BitTagsInputRepeatedAnnouncementIsCarriedByItsOwnElementTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Duplicates, true);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // The message sits in an element of its own, which is keyed so that the very same message said
        // twice in a row replaces the element rather than leaving the live region untouched - a region
        // whose content did not change is a region nothing is read out of.
        Assert.AreEqual("apple added.", com.Find(".bit-tgi-lvr span").TextContent.Trim());

        input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple added.", com.Find(".bit-tgi-lvr span").TextContent.Trim());
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputAnnouncesAnAddedTagTest()
    {
        var com = RenderComponent<BitTagsInput>();

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple added.", com.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputAnnouncesABatchAsACountTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { "," });
        });

        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "a,b,c" });

        // A pasted list read out name by name is not a confirmation but a wall.
        Assert.AreEqual("3 tags added.", com.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputABatchOfOneIsStillNamedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { "," });
        });

        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "apple," });

        Assert.AreEqual("apple added.", com.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputCustomBatchAnnouncementTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { "," });
            parameters.Add(p => p.AddedManyAnnouncementFormat, "{0} tags were added to the list.");
        });

        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "a,b" });

        Assert.AreEqual("2 tags were added to the list.", com.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    #endregion

    #region adding

    [TestMethod]
    public async Task BitTagsInputOnAddCallbackTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "newtag" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("newtag", addedTag);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputTrimsTheTagTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "   spaced   " });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("spaced", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputNoTrimKeepsTheWhitespaceTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.NoTrim, true);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = " spaced " });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(" spaced ", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputWhitespaceOnlyEntryIsNotAddedTest()
    {
        var addCount = 0;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(_ => addCount++));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "    " });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, addCount);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputAddsOnBlurByDefaultTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.FocusOutAsync(new FocusEventArgs());

        Assert.AreEqual("apple", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputNoAddOnBlurTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.NoAddOnBlur, true);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.FocusOutAsync(new FocusEventArgs());

        Assert.IsNull(addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputAddsOnTabTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });

        Assert.AreEqual("apple", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputShiftTabDoesNotAddTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });

        Assert.IsNull(addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputNoAddOnTabTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.NoAddOnTab, true);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });

        Assert.IsNull(addedTag);
        Assert.IsTrue(com.Find(".bit-tgi-inp").HasAttribute("data-no-add-on-tab"));
    }

    [TestMethod]
    public async Task BitTagsInputSeparatorKeyAddsTheTagTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { "," });
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "," });

        Assert.AreEqual("apple", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputSpaceSeparatorTest()
    {
        IReadOnlyList<string>? addedTags = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { " " });
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(t => addedTags = t));
        });

        // A single space is an ordinary separator (a field of words), so it must survive the filtering
        // that only the empty string is meant to fall to.
        Assert.AreEqual("[\" \"]", com.Find(".bit-tgi-inp").GetAttribute("data-separators"));

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "red green blue" });

        Assert.IsNotNull(addedTags);
        CollectionAssert.AreEqual(new[] { "red", "green", "blue" }, addedTags.ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputSpaceSeparatorKeyAddsTheTagTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { " " });
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(t => addedTag = t.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = " " });

        Assert.AreEqual("apple", addedTag);
    }

    [TestMethod]
    public void BitTagsInputEmptySeparatorsAreDroppedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { ",", "", ";" });
        });

        Assert.AreEqual("[\",\",\";\"]", com.Find(".bit-tgi-inp").GetAttribute("data-separators"));
    }

    [TestMethod]
    public async Task BitTagsInputMixedSeparatorPasteTest()
    {
        IReadOnlyList<string>? addedTags = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { ",", ";" });
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(t => addedTags = t));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "a,b;c" });

        Assert.IsNotNull(addedTags);
        Assert.AreEqual(3, addedTags.Count);
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, addedTags.ToArray());
        Assert.AreEqual(3, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputMultiCharacterSeparatorPasteTest()
    {
        IReadOnlyList<string>? addedTags = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { ", " });
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(t => addedTags = t));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "red, green, blue" });

        Assert.IsNotNull(addedTags);
        CollectionAssert.AreEqual(new[] { "red", "green", "blue" }, addedTags.ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputRejectedSingleEntryStaysInTheInputTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Separators, new[] { "," });
            parameters.Add(p => p.MinLength, 5);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "ab," });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("ab", com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputMaxTagsTest()
    {
        string? addedTag = null;
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.MaxTags, 2);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(addedTags => addedTag = addedTags.FirstOrDefault()));
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "cherry" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(addedTag);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual("cherry", invalidArgs.Tag);
        Assert.AreEqual(BitTagsInputInvalidReason.MaxTags, invalidArgs.Reason);
    }

    [TestMethod]
    public async Task BitTagsInputMaxTagsStopsAPasteAtTheCeilingTest()
    {
        IReadOnlyList<string>? addedTags = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxTags, 2);
            parameters.Add(p => p.Separators, new[] { "," });
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(t => addedTags = t));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "a,b,c,d" });

        Assert.IsNotNull(addedTags);
        CollectionAssert.AreEqual(new[] { "a", "b" }, addedTags.ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputMaxLengthTruncatesTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxLength, 5);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(addedTags => addedTag = addedTags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "toolongtext" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("toolo", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputMaxLengthTruncatesTheTypedTextTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxLength, 5);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "toolongtext" });

        Assert.AreEqual("toolo", com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputMinLengthTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MinLength, 4);
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "abc" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.MinLength, invalidArgs.Reason);
    }

    [TestMethod]
    public async Task BitTagsInputPatternTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Pattern, "^[a-z]+$");
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "Abc1" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.Pattern, invalidArgs.Reason);

        invalidArgs = null;
        input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "abc" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNull(invalidArgs);
    }

    [TestMethod]
    public async Task BitTagsInputInvalidPatternIsIgnoredTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Pattern, "([a-z"); // does not compile
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "anything" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputValidatorTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Validator, (Func<string, bool>)(t => t == "allowed"));
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "denied" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.Validator, invalidArgs.Reason);
    }

    [TestMethod]
    public async Task BitTagsInputThrowingValidatorRejectsRatherThanBreakingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Validator, (Func<string, bool>)(_ => throw new InvalidOperationException()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "anything" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputTransformerTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Transformer, (Func<string, string>)(t => t.TrimStart('#').ToLowerInvariant()));
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "#Blazor" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("blazor", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputTransformerRunsBeforeTheDuplicateCheckTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "blazor" });
            parameters.Add(p => p.Transformer, (Func<string, string>)(t => t.ToLowerInvariant()));
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "BLAZOR" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual("blazor", invalidArgs.Tag);
        Assert.AreEqual(BitTagsInputInvalidReason.Duplicate, invalidArgs.Reason);
    }

    [TestMethod]
    public async Task BitTagsInputThrowingTransformerLeavesTheTextUntouchedTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Transformer, (Func<string, string>)(_ => throw new InvalidOperationException()));
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputDuplicateNotAllowedByDefaultTest()
    {
        string? existsTag = null;
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnTagExists, (string tag) => existsTag = tag);
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple", existsTag);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.Duplicate, invalidArgs.Reason);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputDuplicateComparisonIsOrdinalByDefaultTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "APPLE" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputCaseInsensitiveDuplicateComparisonTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.Comparison, StringComparison.OrdinalIgnoreCase);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "APPLE" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputDuplicateAllowedTest()
    {
        string? existsTag = null;
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.Duplicates, true);
            parameters.Add(p => p.OnTagExists, (string tag) => existsTag = tag);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(addedTags => addedTag = addedTags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(existsTag);
        Assert.AreEqual("apple", addedTag);
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputOnBeforeAddCancelTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnBeforeAdd, (BitTagsInputBeforeArgs args) => args.Cancel = true);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(addedTags => addedTag = addedTags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "blocked" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(addedTag);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputOnInputCallbackTest()
    {
        string? typed = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnInput, (string text) => typed = text);
        });

        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "app" });

        Assert.AreEqual("app", typed);
    }

    [TestMethod]
    public async Task BitTagsInputOnKeyDownCallbackTest()
    {
        string? key = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnKeyDown, (KeyboardEventArgs e) => key = e.Key);
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual("a", key);
    }

    [TestMethod]
    public async Task BitTagsInputReadOnlyDoesNotAddTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputReadOnlyDoesNotAddOnBlurTest()
    {
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(tags => addedTag = tags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });

        com.Render(parameters => parameters.Add(p => p.ReadOnly, true));

        await com.Find(".bit-tgi-inp").FocusOutAsync(new FocusEventArgs());

        Assert.IsNull(addedTag);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputCommittingATagReportsTheEmptiedInputTest()
    {
        var reported = new List<string>();

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnInput, (string text) => reported.Add(text));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // A suggestion list of the consumer driven by OnInput would otherwise be left filtering on the very
        // word that has just become a tag, and would go on offering it.
        CollectionAssert.AreEqual(new[] { "apple", string.Empty }, reported.ToArray());
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputClearOnBlurDiscardsWhatCouldNotBeAddedTest()
    {
        string? typed = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ClearOnBlur, true);
            parameters.Add(p => p.MinLength, 5);
            parameters.Add(p => p.OnInput, (string text) => typed = text);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "ab" });
        await input.FocusOutAsync(new FocusEventArgs());

        // The text had its chance to become a tag first and was refused, so what ClearOnBlur takes away is
        // only what was left over.
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
        Assert.AreEqual(string.Empty, typed);
    }

    [TestMethod]
    public async Task BitTagsInputClearOnBlurKeepsTheTagItCouldAddTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ClearOnBlur, true);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.FocusOutAsync(new FocusEventArgs());

        // ClearOnBlur throws away the leftover text, never the tag the very same blur has just made of it.
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent);
    }

    [TestMethod]
    public async Task BitTagsInputClearOnBlurWithNoAddOnBlurCancelsTheTypingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ClearOnBlur, true);
            parameters.Add(p => p.NoAddOnBlur, true);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.FocusOutAsync(new FocusEventArgs());

        // Paired with NoAddOnBlur, leaving the field cancels what was being typed outright.
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputClearOnBlurIsInertWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ClearOnBlur, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.FocusOutAsync(new FocusEventArgs());

        // A read-only field changes nothing of its own accord, the text in its input included.
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    #endregion

    #region removing

    [TestMethod]
    public async Task BitTagsInputOnRemoveCallbackTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        await com.Find(".bit-tgi-dbt").ClickAsync(new MouseEventArgs());

        Assert.AreEqual("apple", removedTag);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRemovesTheRightTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana", "cherry" });
        });

        await com.FindAll(".bit-tgi-dbt")[1].ClickAsync(new MouseEventArgs());

        var texts = com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray();

        CollectionAssert.AreEqual(new[] { "apple", "cherry" }, texts);
    }

    [TestMethod]
    public async Task BitTagsInputOnBeforeRemoveCancelTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnBeforeRemove, (BitTagsInputBeforeArgs args) => args.Cancel = true);
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        await com.Find(".bit-tgi-dbt").ClickAsync(new MouseEventArgs());

        Assert.IsNull(removedTag);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputACancelledDismissLeavesTheFocusAloneTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnBeforeRemove, (BitTagsInputBeforeArgs args) => args.Cancel = true);
        });

        // The tab stop is walked onto the second tag, and a removal that is called off must not hand it
        // back to the input: nothing happened, so nothing about the user's place should change either.
        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });
        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));

        await com.FindAll(".bit-tgi-dbt")[1].ClickAsync(new MouseEventArgs());

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputBackspaceRemovesTheLastTagTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        Assert.AreEqual("banana", removedTag);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputBackspaceDoesNotRemoveWhileTypingTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "x" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        Assert.IsNull(removedTag);
    }

    [TestMethod]
    public async Task BitTagsInputNoBackspaceRemoveTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.NoBackspaceRemove, true);
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        Assert.IsNull(removedTag);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRemovingTheLastTagLeavesTheValueNullTest()
    {
        ICollection<string>? current = new List<string> { "apple" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, current);
            parameters.Add(p => p.ValueChanged, (ICollection<string>? v) => current = v);
        });

        await com.Find(".bit-tgi-dbt").ClickAsync(new MouseEventArgs());

        Assert.IsNull(current);
    }

    [TestMethod]
    public async Task BitTagsInputBackspaceEditsTheLastTagTest()
    {
        string? removedTag = null;
        string? typed = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.BackspaceEditsLastTag, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
            parameters.Add(p => p.OnInput, (string text) => typed = text);
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        // The tag is taken off the list and put back where it was typed, so the key corrects rather than
        // destroys: a tag removed by a keystroke has no undo of its own.
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent);
        Assert.AreEqual("banana", removedTag);
        Assert.AreEqual("banana", com.Find(".bit-tgi-inp").GetAttribute("value"));
        Assert.AreEqual("banana", typed);
    }

    [TestMethod]
    public async Task BitTagsInputBackspaceEditIsCommittedBackTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.BackspaceEditsLastTag, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var input = com.Find(".bit-tgi-inp");
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });
        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "apples" });
        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // The corrected text goes back in as an ordinary tag, which is the whole point of taking it out.
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("apples", com.Find(".bit-tgi-ttx").TextContent);
    }

    [TestMethod]
    public async Task BitTagsInputNoBackspaceRemoveWinsOverTheBackspaceEditTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.BackspaceEditsLastTag, true);
            parameters.Add(p => p.NoBackspaceRemove, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        // A field that was told the key does nothing keeps it doing nothing, however it would otherwise act.
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputACancelledBackspaceEditLeavesTheInputEmptyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.BackspaceEditsLastTag, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnBeforeRemove, (BitTagsInputBeforeArgs args) => args.Cancel = true);
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        // The tag is still in the list, so its text sitting in the input next to it would only ever be
        // added back as a duplicate.
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputBackspaceEditOnAnEmptyListDoesNothingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.BackspaceEditsLastTag, true);
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    #endregion

    #region clear button

    [TestMethod]
    public void BitTagsInputClearButtonIsNotRenderedWhenEmptyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-cbt").Count);
    }

    [TestMethod]
    public void BitTagsInputClearButtonIsNotRenderedWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-cbt").Count);
    }

    [TestMethod]
    public async Task BitTagsInputClearButtonTest()
    {
        IReadOnlyList<string>? cleared = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnClear, (Action<IReadOnlyList<string>>)(tags => cleared = tags));
        });

        var button = com.Find(".bit-tgi-cbt");

        Assert.AreEqual("Clear all tags", button.GetAttribute("aria-label"));
        Assert.AreEqual("-1", button.GetAttribute("tabindex"));

        await button.ClickAsync(new MouseEventArgs());

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(cleared);
        CollectionAssert.AreEqual(new[] { "apple", "banana" }, cleared.ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputClearAnnouncesACountTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana", "cherry" });
        });

        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());

        // A count rather than every name: reading a long list out is not a confirmation but a wall.
        Assert.AreEqual("3 tags removed.", com.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputCustomClearedAnnouncementTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.ClearedAnnouncementFormat, "The list of {0} tags was emptied.");
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());

        Assert.AreEqual("The list of 2 tags was emptied.", com.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputEscapeClearsWhenTheClearButtonIsShownTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEscapeDoesNothingWithoutTheClearButtonTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEscapeTakesBackTheTypedTextBeforeTheListTest()
    {
        string? typed = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnInput, (string text) => typed = text);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "cher" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        // Throwing a whole list of tags away over a half typed word is not an undo but a loss, so the
        // first press only takes the word back.
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
        Assert.AreEqual(string.Empty, typed);
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEscapeTakesTheTypedTextBackWithoutTheClearButtonTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "cher" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        // Taking the typed text back is not the clear button's job, so it does not depend on it.
        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputOnBeforeClearCancelTest()
    {
        IReadOnlyList<string>? offered = null;
        var cleared = 0;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnBeforeClear, (BitTagsInputClearArgs args) => { offered = args.Tags; args.Cancel = true; });
            parameters.Add(p => p.OnClear, (Action<IReadOnlyList<string>>)(_ => cleared++));
        });

        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());

        Assert.IsNotNull(offered);
        CollectionAssert.AreEqual(new[] { "apple", "banana" }, offered.ToArray());
        Assert.AreEqual(0, cleared);
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputOnBeforeClearGuardsEveryWayOfClearingTest()
    {
        var attempts = 0;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnBeforeClear, (BitTagsInputClearArgs args) => { attempts++; args.Cancel = true; });
        });

        // The button, the Escape key and the method all go through the very same gate.
        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());
        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        await com.Instance.Clear();

        Assert.AreEqual(3, attempts);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputClearingAnEmptyFieldChangesNothingTest()
    {
        var changes = 0;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, (ICollection<string>?)null);
            parameters.Add(p => p.OnChange, (ICollection<string>? _) => changes++);
        });

        await com.Instance.Clear();

        // Setting an already empty value again would mark the form dirty over a list that did not change.
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task BitTagsInputClearMethodTest()
    {
        ICollection<string>? tags = new List<string> { "apple", "banana" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, tags);
            parameters.Add(p => p.OnChange, (ICollection<string>? v) => tags = v);
        });

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);

        await com.InvokeAsync(() => com.Instance.Clear());

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNull(tags);
    }

    [TestMethod]
    public async Task BitTagsInputClearButtonHandsTheFocusBackToTheInputTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        var focusCalls = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());

        // Emptying the field takes the clear button away with it, so the focus would otherwise be dropped
        // on the document body and the user would lose their place entirely.
        Assert.AreEqual(0, com.FindAll(".bit-tgi-cbt").Count);
        Assert.AreEqual(focusCalls + 1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public async Task BitTagsInputClearReportsTheEmptiedInputTest()
    {
        var reported = new List<string>();

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.OnInput, (string text) => reported.Add(text));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "app" });
        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());

        // Clearing the field empties its input as well, and an emptying the consumer is not told about is
        // one their suggestion list goes on ignoring.
        CollectionAssert.AreEqual(new[] { "app", string.Empty }, reported.ToArray());
    }

    #endregion

    #region tag keyboard navigation

    [TestMethod]
    public async Task BitTagsInputArrowKeysMoveTheRovingTabStopTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana", "cherry" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));

        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[2].GetAttribute("tabindex"));

        await com.FindAll(".bit-tgi-tag")[2].KeyDownAsync(new KeyboardEventArgs { Key = "Home" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputArrowKeysAreMirroredInRtlTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        // In a right-to-left direction the key pointing at the visual left walks forward.
        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputDeleteRemovesTheFocusedTagTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana", "cherry" });
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });

        Assert.AreEqual("banana", removedTag);

        var texts = com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "apple", "cherry" }, texts);
    }

    [TestMethod]
    public async Task BitTagsInputBackspaceOnAFocusedTagRemovesItTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "Backspace" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputReadOnlyTagKeyboardRemovalIsBlockedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputBackwardArrowFromAnEmptyInputWalksIntoTheLastTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    #endregion

    #region suggestions

    [TestMethod]
    public void BitTagsInputNoDatalistWithoutSuggestionsTest()
    {
        var com = RenderComponent<BitTagsInput>();

        Assert.AreEqual(0, com.FindAll("datalist").Count);
        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("list"));
    }

    [TestMethod]
    public void BitTagsInputSuggestionsRenderADatalistTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "apple", "banana" });
        });

        var datalist = com.Find("datalist");

        Assert.AreEqual(datalist.Id, com.Find(".bit-tgi-inp").GetAttribute("list"));
        CollectionAssert.AreEqual(new[] { "apple", "banana" }, datalist.QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToArray());
    }

    [TestMethod]
    public void BitTagsInputSuggestionsDropTheOnesAlreadyAddedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "apple", "banana" });
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        CollectionAssert.AreEqual(new[] { "banana" }, com.Find("datalist").QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToArray());
    }

    [TestMethod]
    public void BitTagsInputSuggestionsKeepTheOnesAlreadyAddedWhenDuplicatesAreAllowedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "apple", "banana" });
            parameters.Add(p => p.Duplicates, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual(2, com.Find("datalist").QuerySelectorAll("option").Length);
    }

    [TestMethod]
    public void BitTagsInputSuggestionsAreDroppedAtTheCeilingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "apple", "banana", "cherry" });
            parameters.Add(p => p.MaxTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        // Nothing else can be added, so offering a value would only be a dead end.
        Assert.AreEqual(0, com.FindAll("datalist").Count);
        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("list"));
    }

    [TestMethod]
    public void BitTagsInputMaxSuggestionsCapsWhatIsWrittenIntoThePageTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxSuggestions, 2);
            parameters.Add(p => p.Suggestions, new[] { "apple", "apricot", "avocado", "banana" });
        });

        // A catalogue is not written into the page in full; the browser filters what it is handed anyway.
        var options = com.FindAll("datalist option");

        Assert.AreEqual(2, options.Count);
        Assert.AreEqual("apple", options[0].GetAttribute("value"));
        Assert.AreEqual("apricot", options[1].GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputMaxSuggestionsKeepsTheOnesThatMatchWhatIsTypedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxSuggestions, 2);
            parameters.Add(p => p.Suggestions, new[] { "apple", "apricot", "avocado", "banana", "blueberry" });
        });

        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "b" });

        // The values that are kept are the ones that could still be picked rather than the first few of
        // the alphabet.
        var options = com.FindAll("datalist option");

        Assert.AreEqual(2, options.Count);
        Assert.AreEqual("banana", options[0].GetAttribute("value"));
        Assert.AreEqual("blueberry", options[1].GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputMaxSuggestionsMatchingFollowsTheComparisonTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxSuggestions, 3);
            parameters.Add(p => p.Comparison, StringComparison.OrdinalIgnoreCase);
            parameters.Add(p => p.Suggestions, new[] { "Apple", "Banana" });
        });

        await com.Find(".bit-tgi-inp").InputAsync(new ChangeEventArgs { Value = "app" });

        Assert.AreEqual(1, com.FindAll("datalist option").Count);
        Assert.AreEqual("Apple", com.Find("datalist option").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTagsInputNoMaxSuggestionsOffersEveryValueTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "apple", "apricot", "avocado", "banana" });
        });

        Assert.AreEqual(4, com.FindAll("datalist option").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRestrictToSuggestionsTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "blazor", "react" });
            parameters.Add(p => p.RestrictToSuggestions, true);
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "svelte" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual("svelte", invalidArgs.Tag);
        Assert.AreEqual(BitTagsInputInvalidReason.NotSuggested, invalidArgs.Reason);

        invalidArgs = null;
        input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "blazor" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(invalidArgs);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRestrictToSuggestionsUsesTheComparisonTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "blazor" });
            parameters.Add(p => p.RestrictToSuggestions, true);
            parameters.Add(p => p.Comparison, StringComparison.OrdinalIgnoreCase);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "BLAZOR" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRestrictToSuggestionsStoresTheSuggestedSpellingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "blazor" });
            parameters.Add(p => p.RestrictToSuggestions, true);
            parameters.Add(p => p.Comparison, StringComparison.OrdinalIgnoreCase);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "BLAZOR" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // A field whose only accepted values are the suggestions must not end up holding two spellings
        // of one value: what is stored is the suggestion, not the way it happened to be typed.
        Assert.AreEqual("blazor", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputSuggestedSpellingIsNotAppliedWithoutTheRestrictionTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Suggestions, new[] { "blazor" });
            parameters.Add(p => p.Comparison, StringComparison.OrdinalIgnoreCase);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "BLAZOR" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // A datalist suggests rather than restricts, so free text keeps the spelling it was given.
        Assert.AreEqual("BLAZOR", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputRestrictToSuggestionsWithoutSuggestionsAcceptsNothingTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.RestrictToSuggestions, true);
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "anything" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.NotSuggested, invalidArgs.Reason);
    }

    [TestMethod]
    public async Task BitTagsInputRestrictToSuggestionsAppliesToTheInlineEditTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.Suggestions, new[] { "blazor", "react" });
            parameters.Add(p => p.RestrictToSuggestions, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "blazor" });
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "svelte" });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.NotSuggested, invalidArgs.Reason);
        Assert.AreEqual("blazor", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputTagsListIsNamedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual("Tags", com.Find(".bit-tgi-tgs").GetAttribute("aria-label"));

        com.Render(parameters => parameters.Add(p => p.TagsAriaLabel, "Selected skills"));

        Assert.AreEqual("Selected skills", com.Find(".bit-tgi-tgs").GetAttribute("aria-label"));
    }

    #endregion

    #region editable tags

    [TestMethod]
    public async Task BitTagsInputNoEditWithoutEditableTagsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").DoubleClickAsync(new MouseEventArgs());

        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
    }

    [TestMethod]
    public async Task BitTagsInputDoubleClickOpensTheInlineEditTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.FindAll(".bit-tgi-tag")[1].DoubleClickAsync(new MouseEventArgs());

        var editInput = com.Find(".bit-tgi-eip");

        Assert.AreEqual("banana", editInput.GetAttribute("value"));
        Assert.AreEqual("Edit banana", editInput.GetAttribute("aria-label"));

        // The dismiss button steps aside while the tag is an input, and the tag gives up its tab stop.
        Assert.AreEqual(1, com.FindAll(".bit-tgi-dbt").Count);
        Assert.AreEqual("-1", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputF2OpensTheInlineEditTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-eip").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEnterOnAFocusedTagOpensTheInlineEditTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-eip").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEditCommitsOnEnterTest()
    {
        BitTagsInputEditArgs? editArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnEdit, (BitTagsInputEditArgs args) => editArgs = args);
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "  apricot  " });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);

        var texts = com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "apricot", "banana" }, texts);

        Assert.IsNotNull(editArgs);
        Assert.AreEqual("apple", editArgs.Tag);
        Assert.AreEqual("apricot", editArgs.NewTag);
    }

    [TestMethod]
    public async Task BitTagsInputEditIsCancelledByEscapeTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "changed" });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputEditCommitsOnFocusOutTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "apricot" });
        await editInput.FocusOutAsync(new FocusEventArgs());

        Assert.AreEqual("apricot", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputCommitOnFocusOutDoesNotPullTheFocusBackTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "apricot" });
        await editInput.FocusOutAsync(new FocusEventArgs());

        // The commit only happened because the focus went somewhere else of its own accord; pulling it
        // back into the field would undo the very move that caused it. The tab stop of the list still
        // belongs on the tag that was corrected, though.
        Assert.AreEqual("apricot", com.FindAll(".bit-tgi-ttx")[1].TextContent.Trim());
        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputEditToAnEmptyTextRemovesTheTagTest()
    {
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "   " });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple", removedTag);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEditIsValidatedTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.MinLength, 4);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "ab" });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.MinLength, invalidArgs.Reason);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputEditIsNotADuplicateOfItselfTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.MaxTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "apricot" });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        // The list neither grew (so the MaxTags ceiling is not reached) nor collided with itself.
        Assert.IsNull(invalidArgs);
        Assert.AreEqual("apricot", com.FindAll(".bit-tgi-ttx")[0].TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputEditIntoAnExistingTagIsRefusedTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "banana" });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.Duplicate, invalidArgs.Reason);
        Assert.AreEqual("apple", com.FindAll(".bit-tgi-ttx")[0].TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputOnEditCancelTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnEdit, (BitTagsInputEditArgs args) => args.Cancel = true);
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        var editInput = com.Find(".bit-tgi-eip");
        await editInput.InputAsync(new ChangeEventArgs { Value = "apricot" });
        await editInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputEditInputCarriesTheMaxLengthTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        Assert.IsFalse(com.Find(".bit-tgi-eip").HasAttribute("maxlength"));

        await com.Find(".bit-tgi-eip").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        com.Render(parameters => parameters.Add(p => p.MaxLength, 5));

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        // The limit of a tag is the limit of the whole of this input, so the browser can carry it.
        Assert.AreEqual("5", com.Find(".bit-tgi-eip").GetAttribute("maxlength"));
    }

    [TestMethod]
    public async Task BitTagsInputRemovingTheEditedTagClosesTheEditTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-eip").Count);

        await com.Instance.RemoveTagAtAsync(1);

        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputEditFollowsTheTagItIsOpenOnTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana", "cherry" });
        });

        await com.FindAll(".bit-tgi-tag")[2].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        // The tag that was being corrected has shifted one place down, and the edit has to shift with
        // it rather than stay open on whatever now stands where it was.
        await com.Instance.RemoveTagAtAsync(0);

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.AreEqual(2, tags.Count);
        Assert.AreEqual(1, tags[1].QuerySelectorAll(".bit-tgi-eip").Length);
        Assert.AreEqual("cherry", com.Find(".bit-tgi-eip").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputEditFollowsAMovedTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana", "cherry" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        await com.Instance.MoveTagAsync(0, 2);

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.AreEqual(1, tags[2].QuerySelectorAll(".bit-tgi-eip").Length);
        Assert.AreEqual("apple", com.Find(".bit-tgi-eip").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputEditIsBlockedWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
    }

    [TestMethod,
        DataRow("ReadOnly"),
        DataRow("IsEnabled"),
        DataRow("EditableTags")]
    public async Task BitTagsInputAnOpenEditIsClosedWhenTheFieldTurnsInertTest(string parameter)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });

        Assert.AreEqual(1, com.FindAll(".bit-tgi-eip").Count);

        com.Render(parameters =>
        {
            switch (parameter)
            {
                case "ReadOnly": parameters.Add(p => p.ReadOnly, true); break;
                case "IsEnabled": parameters.Add(p => p.IsEnabled, false); break;
                default: parameters.Add(p => p.EditableTags, false); break;
            }
        });

        // The little input would otherwise go on taking text in a field that no longer accepts any.
        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent);
    }

    [TestMethod]
    public async Task BitTagsInputAnEditIsNotCommittedByAFieldThatTurnedInertTest()
    {
        var edits = 0;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.OnEdit, (BitTagsInputEditArgs _) => edits++);
        });

        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });
        await com.Find(".bit-tgi-eip").InputAsync(new ChangeEventArgs { Value = "apricot" });
        await com.Find(".bit-tgi-eip").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        // Reopening the edit and then making the field read-only under it: the correction is dropped rather
        // than committed through the back door by the focus leaving the input.
        await com.Find(".bit-tgi-tag").KeyDownAsync(new KeyboardEventArgs { Key = "F2" });
        await com.Find(".bit-tgi-eip").InputAsync(new ChangeEventArgs { Value = "apricot" });

        com.Render(parameters => parameters.Add(p => p.ReadOnly, true));

        Assert.AreEqual(0, edits);
        Assert.AreEqual("apple", com.Find(".bit-tgi-ttx").TextContent);
    }

    #endregion

    #region counter

    [TestMethod]
    public void BitTagsInputCounterIsNotRenderedByDefaultTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-cnr").Count);
    }

    [TestMethod]
    public void BitTagsInputCounterTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowCounter, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        Assert.AreEqual("2", com.Find(".bit-tgi-cnr").TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputCounterWithMaxTagsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowCounter, true);
            parameters.Add(p => p.MaxTags, 5);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        Assert.AreEqual("2/5", com.Find(".bit-tgi-cnr").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputCounterFollowsTheListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ShowCounter, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "banana" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("2", com.Find(".bit-tgi-cnr").TextContent.Trim());
    }

    #endregion

    #region folding the tags away

    [TestMethod]
    public void BitTagsInputNoToggleWithoutMaxDisplayedTagsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tgl").Count);
        Assert.AreEqual(3, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public void BitTagsInputNoToggleWhileTheListFitsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 3);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tgl").Count);
        Assert.AreEqual(3, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public void BitTagsInputMaxDisplayedTagsFoldsTheRestAwayTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d", "e" });
        });

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.AreEqual(2, tags.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());

        var toggle = com.Find(".bit-tgi-tgl");

        Assert.AreEqual("+3", toggle.TextContent.Trim());
        Assert.AreEqual("false", toggle.GetAttribute("aria-expanded"));

        // "+3" read out on its own says nothing about what pressing it would do, and the chip has to
        // say which of the parts of the field it unfolds.
        Assert.AreEqual("Show 3 more tags", toggle.GetAttribute("aria-label"));
        Assert.AreEqual(com.Find(".bit-tgi-tgs").Id, toggle.GetAttribute("aria-controls"));
    }

    [TestMethod]
    public async Task BitTagsInputCustomMoreTagsAriaLabelTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 1);
            parameters.Add(p => p.MoreTagsAriaLabelFormat, "{0} weitere anzeigen");
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        Assert.AreEqual("2 weitere anzeigen", com.Find(".bit-tgi-tgl").GetAttribute("aria-label"));

        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        // Once unfolded, the visible label is a proper name of its own.
        Assert.IsFalse(com.Find(".bit-tgi-tgl").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTagsInputFoldingDoesNotTouchTheValueTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Name, "tags");
            parameters.Add(p => p.ShowCounter, true);
            parameters.Add(p => p.MaxDisplayedTags, 1);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        // Only how much of the list is drawn changes: the value, the counter and the posted field all
        // keep every one of the tags.
        Assert.AreEqual(3, com.Instance.Value!.Count);
        Assert.AreEqual("3", com.Find(".bit-tgi-cnr").TextContent.Trim());
        Assert.AreEqual("a,b,c", com.Find("input[type=hidden]").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputToggleUnfoldsAndFoldsTheListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.LessTagsText, "Show less");
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        Assert.AreEqual(4, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("Show less", com.Find(".bit-tgi-tgl").TextContent.Trim());
        Assert.AreEqual("true", com.Find(".bit-tgi-tgl").GetAttribute("aria-expanded"));

        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("+2", com.Find(".bit-tgi-tgl").TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputCustomFoldingLabelsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 1);
            parameters.Add(p => p.MoreTagsFormat, "{0} more...");
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        Assert.AreEqual("2 more...", com.Find(".bit-tgi-tgl").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputFoldedTagsKeepTheRovingTabStopReachableTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        // The tab stop is walked onto a tag that is only drawn while the list is unfolded...
        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[3].GetAttribute("tabindex"));

        // ...so folding the list back has to hand it to one that is still there.
        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.AreEqual(2, tags.Count);
        Assert.AreEqual(1, tags.Count(t => t.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("0", tags[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputNavigationStopsAtTheFoldTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        // End lands on the last tag that is drawn rather than on one that is folded away.
        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));

        // And walking forward off it goes back to the input, not into the fold.
        await com.FindAll(".bit-tgi-tag")[1].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputBackwardArrowLandsOnTheLastDrawnTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputClearFoldsTheListBackTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        Assert.AreEqual(4, com.FindAll(".bit-tgi-tag").Count);

        await com.Find(".bit-tgi-cbt").ClickAsync(new MouseEventArgs());
        await com.Instance.AddTagsAsync(["a", "b", "c", "d"]);

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputAMovePastTheFoldUnfoldsTheListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "End", AltKey = true });

        // The tag would otherwise vanish behind the fold along with the focus that follows it.
        CollectionAssert.AreEqual(new[] { "b", "c", "d", "a" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[3].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputAListThatFitsAgainIsFoldedBackTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 2);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c", "d" });
        });

        await com.Find(".bit-tgi-tgl").ClickAsync(new MouseEventArgs());

        Assert.AreEqual(4, com.FindAll(".bit-tgi-tag").Count);

        await com.Instance.RemoveTagAtAsync(3);
        await com.Instance.RemoveTagAtAsync(2);

        // Nothing is left to unfold, so the chip that unfolded the list is gone...
        Assert.AreEqual(0, com.FindAll(".bit-tgi-tgl").Count);

        await com.Instance.AddTagsAsync(["e", "f"]);

        // ...and an unfolded state that outlived it must not silently ignore the MaxDisplayedTags.
        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("+2", com.Find(".bit-tgi-tgl").TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputToggleIsDisabledAlongWithTheFieldTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxDisplayedTags, 1);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        Assert.IsTrue(com.Find(".bit-tgi-tgl").HasAttribute("disabled"));
    }

    #endregion

    #region tag hint

    [TestMethod]
    public void BitTagsInputNoTagHintForAPlainListTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        // Instructions for gestures the component does not offer would be noise.
        Assert.AreEqual(0, com.FindAll(".bit-tgi-vsh").Count);
        Assert.IsFalse(com.Find(".bit-tgi-tag").HasAttribute("aria-describedby"));
    }

    [TestMethod,
        DataRow(true, false, "Press Enter to edit, or Delete to remove."),
        DataRow(false, true, "Press Alt with the arrow keys to move, or Delete to remove."),
        DataRow(true, true, "Press Enter to edit, Delete to remove, or Alt with the arrow keys to move.")]
    public void BitTagsInputTagHintFollowsTheEnabledGesturesTest(bool editableTags, bool allowReorder, string expected)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, editableTags);
            parameters.Add(p => p.AllowReorder, allowReorder);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var hint = com.Find(".bit-tgi-vsh");

        Assert.AreEqual(expected, hint.TextContent.Trim());
        Assert.AreEqual(hint.Id, com.Find(".bit-tgi-tag").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTagsInputCustomTagHintTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.TagAriaDescription, "Enter zum Bearbeiten.");
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        var hint = com.Find(".bit-tgi-vsh");

        // A hint of the consumer stands on its own, whatever the component was given.
        Assert.AreEqual("Enter zum Bearbeiten.", hint.TextContent.Trim());
        Assert.AreEqual(hint.Id, com.Find(".bit-tgi-tag").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitTagsInputEmptyTagHintIsNotRenderedTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.TagAriaDescription, string.Empty);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-vsh").Count);
        Assert.IsFalse(com.Find(".bit-tgi-tag").HasAttribute("aria-describedby"));
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(false, false)]
    public void BitTagsInputNoTagHintWhenTheFieldIsInertTest(bool readOnly, bool isEnabled)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        Assert.AreEqual(0, com.FindAll(".bit-tgi-vsh").Count);
    }

    #endregion

    #region reordering

    [TestMethod]
    public async Task BitTagsInputNoReorderWithoutAllowReorderTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

        var texts = com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, texts);
    }

    [TestMethod]
    public async Task BitTagsInputAltArrowMovesTheTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

        var texts = com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, texts);

        // The focus travels with the tag, so several steps can be taken in a row.
        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputAltHomeAndEndMoveTheTagToTheEndsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "End", AltKey = true });

        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());

        await com.FindAll(".bit-tgi-tag")[2].KeyDownAsync(new KeyboardEventArgs { Key = "Home", AltKey = true });

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputReorderStopsAtTheEndsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });

        CollectionAssert.AreEqual(new[] { "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputReorderIsMirroredInRtlTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });

        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputReorderIsBlockedWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

        CollectionAssert.AreEqual(new[] { "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void BitTagsInputTagsAreNotDraggableWithoutAllowReorderTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        Assert.IsTrue(com.FindAll(".bit-tgi-tag").All(t => t.HasAttribute("draggable") is false));
    }

    [TestMethod]
    public void BitTagsInputTagsAreDraggableWithAllowReorderTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        Assert.IsTrue(com.FindAll(".bit-tgi-tag").All(t => t.GetAttribute("draggable") == "true"));
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(false, false)]
    public void BitTagsInputTagsAreNotDraggableWhenTheFieldIsInertTest(bool readOnly, bool isEnabled)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        Assert.IsTrue(com.FindAll(".bit-tgi-tag").All(t => t.HasAttribute("draggable") is false));
    }

    [TestMethod]
    public async Task BitTagsInputDropMovesTheTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        await com.FindAll(".bit-tgi-tag")[0].DragStartAsync(new DragEventArgs());
        await com.FindAll(".bit-tgi-tag")[2].DropAsync(new DragEventArgs());

        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());

        // The focus follows the tag, exactly as it does for the keyboard move.
        Assert.AreEqual("0", com.FindAll(".bit-tgi-tag")[2].GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitTagsInputDragMarksTheCarriedAndTheHoveredTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
        });

        await com.FindAll(".bit-tgi-tag")[0].DragStartAsync(new DragEventArgs());
        await com.FindAll(".bit-tgi-tag")[2].DragEnterAsync(new DragEventArgs());

        var tags = com.FindAll(".bit-tgi-tag");

        Assert.IsTrue(tags[0].ClassList.Contains("bit-tgi-tag-drg"));
        Assert.IsFalse(tags[1].ClassList.Contains("bit-tgi-tag-dro"));
        Assert.IsTrue(tags[2].ClassList.Contains("bit-tgi-tag-dro"));

        await com.FindAll(".bit-tgi-tag")[0].DragEndAsync(new DragEventArgs());

        tags = com.FindAll(".bit-tgi-tag");

        Assert.IsFalse(tags[0].ClassList.Contains("bit-tgi-tag-drg"));
        Assert.IsFalse(tags[2].ClassList.Contains("bit-tgi-tag-dro"));
    }

    [TestMethod]
    public async Task BitTagsInputDropOnTheCarriedTagChangesNothingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        await com.FindAll(".bit-tgi-tag")[0].DragStartAsync(new DragEventArgs());
        await com.FindAll(".bit-tgi-tag")[0].DropAsync(new DragEventArgs());

        CollectionAssert.AreEqual(new[] { "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputDropWithoutAllowReorderChangesNothingTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        await com.FindAll(".bit-tgi-tag")[0].DragStartAsync(new DragEventArgs());
        await com.FindAll(".bit-tgi-tag")[1].DropAsync(new DragEventArgs());

        CollectionAssert.AreEqual(new[] { "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputOnReorderReportsTheMoveTest()
    {
        BitTagsInputReorderArgs? reorderArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
            parameters.Add(p => p.OnReorder, (BitTagsInputReorderArgs args) => reorderArgs = args);
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "End", AltKey = true });

        // The bound value alone would have to be diffed against its previous state to find out which
        // of the tags moved and where to.
        Assert.IsNotNull(reorderArgs);
        Assert.AreEqual("a", reorderArgs.Tag);
        Assert.AreEqual(0, reorderArgs.OldIndex);
        Assert.AreEqual(2, reorderArgs.NewIndex);
    }

    [TestMethod]
    public async Task BitTagsInputOnReorderReportsADropTest()
    {
        BitTagsInputReorderArgs? reorderArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
            parameters.Add(p => p.OnReorder, (BitTagsInputReorderArgs args) => reorderArgs = args);
        });

        await com.FindAll(".bit-tgi-tag")[2].DragStartAsync(new DragEventArgs());
        await com.FindAll(".bit-tgi-tag")[0].DropAsync(new DragEventArgs());

        Assert.IsNotNull(reorderArgs);
        Assert.AreEqual("c", reorderArgs.Tag);
        Assert.AreEqual(2, reorderArgs.OldIndex);
        Assert.AreEqual(0, reorderArgs.NewIndex);
    }

    [TestMethod]
    public async Task BitTagsInputNoOnReorderForAMoveThatGoesNowhereTest()
    {
        var reorders = 0;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
            parameters.Add(p => p.OnReorder, (BitTagsInputReorderArgs _) => reorders++);
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });

        Assert.AreEqual(0, reorders);
    }

    [TestMethod]
    public async Task BitTagsInputAltDoesNotRemoveTheTagTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.AllowReorder, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        await com.FindAll(".bit-tgi-tag")[0].KeyDownAsync(new KeyboardEventArgs { Key = "Delete", AltKey = true });

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    #endregion

    #region public API

    [TestMethod]
    public async Task BitTagsInputAddTagAsyncTest()
    {
        var com = RenderComponent<BitTagsInput>();

        await com.Instance.AddTagAsync("apple");

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputAddTagAsyncGoesThroughTheValidationTest()
    {
        BitTagsInputInvalidArgs? invalidArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MinLength, 5);
            parameters.Add(p => p.OnInvalid, (BitTagsInputInvalidArgs args) => invalidArgs = args);
        });

        await com.Instance.AddTagAsync("abc");

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
        Assert.IsNotNull(invalidArgs);
        Assert.AreEqual(BitTagsInputInvalidReason.MinLength, invalidArgs.Reason);
    }

    [TestMethod]
    public async Task BitTagsInputAddTagsAsyncTest()
    {
        var com = RenderComponent<BitTagsInput>();

        await com.Instance.AddTagsAsync(["apple", "banana"]);

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRemoveTagAsyncTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Instance.RemoveTagAsync("apple");

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual("banana", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputRemoveTagAsyncUsesTheComparisonTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Comparison, StringComparison.OrdinalIgnoreCase);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Instance.RemoveTagAsync("APPLE");

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputRemoveTagAtAsyncIgnoresAnOutOfRangeIndexTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Instance.RemoveTagAtAsync(7);

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod]
    public async Task BitTagsInputMoveTagAsyncTest()
    {
        BitTagsInputReorderArgs? reorderArgs = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b", "c" });
            parameters.Add(p => p.OnReorder, (BitTagsInputReorderArgs args) => reorderArgs = args);
        });

        // AllowReorder is what offers the gesture to the user, not what permits the list to be reordered.
        await com.Instance.MoveTagAsync(2, 0);

        CollectionAssert.AreEqual(new[] { "c", "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
        Assert.IsNotNull(reorderArgs);
        Assert.AreEqual("c", reorderArgs.Tag);
    }

    [TestMethod]
    public async Task BitTagsInputMoveTagAsyncIgnoresAnOutOfRangeIndexTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "a", "b" });
        });

        await com.Instance.MoveTagAsync(0, 7);
        await com.Instance.MoveTagAsync(-1, 1);
        await com.Instance.MoveTagAsync(1, 1);

        CollectionAssert.AreEqual(new[] { "a", "b" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputEditTagAsyncTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Instance.EditTagAsync(1);

        Assert.AreEqual(1, com.FindAll(".bit-tgi-eip").Count);
        Assert.AreEqual("banana", com.Find(".bit-tgi-eip").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputEditTagAsyncNeedsEditableTagsTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Instance.EditTagAsync(0);

        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
    }

    [TestMethod]
    public async Task BitTagsInputSetInputTextAsyncTest()
    {
        string? typed = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.OnInput, (string text) => typed = text);
        });

        await com.Instance.SetInputTextAsync("blazor");

        Assert.AreEqual("blazor", com.Find(".bit-tgi-inp").GetAttribute("value"));
        Assert.AreEqual("blazor", typed);

        // The very same text is what the confirm key turns into a tag, so nothing skips the pipeline.
        await com.Find(".bit-tgi-inp").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("blazor", com.Find(".bit-tgi-ttx").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitTagsInputSetInputTextAsyncAppliesTheMaxLengthTest()
    {
        string? typed = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.MaxLength, 4);
            parameters.Add(p => p.OnInput, (string text) => typed = text);
        });

        await com.Instance.SetInputTextAsync("toolongtext");

        Assert.AreEqual("tool", com.Find(".bit-tgi-inp").GetAttribute("value"));
        Assert.AreEqual("tool", typed);
    }

    [TestMethod]
    public async Task BitTagsInputSetInputTextAsyncIsInertWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
        });

        await com.Instance.SetInputTextAsync("blazor");

        Assert.AreEqual(string.Empty, com.Find(".bit-tgi-inp").GetAttribute("value"));
    }

    [TestMethod]
    public async Task BitTagsInputNullTagsAreDroppedRatherThanThrownOnTest()
    {
        var com = RenderComponent<BitTagsInput>();

        await com.Instance.AddTagAsync(null!);

        Assert.AreEqual(0, com.FindAll(".bit-tgi-tag").Count);

        await com.Instance.AddTagsAsync(["apple", null!, "banana"]);

        CollectionAssert.AreEqual(new[] { "apple", "banana" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputPublicApiIsInertWhenDisabledTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Instance.AddTagAsync("cherry");
        await com.Instance.RemoveTagAsync("apple");
        await com.Instance.MoveTagAsync(0, 1);
        await com.Instance.EditTagAsync(0);
        await com.Instance.Clear();

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
        CollectionAssert.AreEqual(new[] { "apple", "banana" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public async Task BitTagsInputPublicApiIsInertWhenReadOnlyTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.EditableTags, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple", "banana" });
        });

        await com.Instance.AddTagAsync("cherry");
        await com.Instance.RemoveTagAsync("apple");
        await com.Instance.RemoveTagAtAsync(0);
        await com.Instance.MoveTagAsync(0, 1);
        await com.Instance.EditTagAsync(0);
        await com.Instance.Clear();

        Assert.AreEqual(2, com.FindAll(".bit-tgi-tag").Count);
        Assert.AreEqual(0, com.FindAll(".bit-tgi-eip").Count);
        CollectionAssert.AreEqual(new[] { "apple", "banana" }, com.FindAll(".bit-tgi-ttx").Select(t => t.TextContent.Trim()).ToArray());
    }

    #endregion

    #region binding & validation

    [TestMethod]
    public async Task BitTagsInputTwoWayBindingTest()
    {
        ICollection<string>? bound = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, bound);
            parameters.Add(p => p.ValueChanged, (ICollection<string>? v) => bound = v);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNotNull(bound);
        CollectionAssert.AreEqual(new[] { "apple" }, bound.ToArray());
    }

    [TestMethod]
    public void BitTagsInputShouldRespectDefaultValue()
    {
        var defaultValue = new List<string> { "apple", "banana", "cherry" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.IsNotNull(com.Instance.Value);
        Assert.AreEqual(defaultValue.Count, com.Instance.Value.Count);
        CollectionAssert.AreEqual(defaultValue, com.Instance.Value.ToList());
        Assert.AreEqual(defaultValue.Count, com.FindAll(".bit-tgi-tag").Count);
    }

    [TestMethod,
        DataRow(null)]
    public void BitTagsInputValidationFormInvalidTest(string[]? tags)
    {
        var model = new BitTagsInputTestModel
        {
            Tags = tags is null ? null : new List<string>(tags)
        };

        var com = RenderComponent<BitTagsInputValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, model);
            parameters.Add(p => p.IsEnabled, true);
        });

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(0, com.Instance.ValidCount);
        Assert.AreEqual(1, com.Instance.InvalidCount);
    }

    [TestMethod]
    public void BitTagsInputValidationFormValidTest()
    {
        var model = new BitTagsInputTestModel
        {
            Tags = new List<string> { "valid-tag" }
        };

        var com = RenderComponent<BitTagsInputValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, model);
            parameters.Add(p => p.IsEnabled, true);
        });

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(1, com.Instance.ValidCount);
        Assert.AreEqual(0, com.Instance.InvalidCount);
    }

    [TestMethod]
    public void BitTagsInputValidationInvalidHtmlAttributeTest()
    {
        var model = new BitTagsInputTestModel { Tags = null };

        var com = RenderComponent<BitTagsInputValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, model);
            parameters.Add(p => p.IsEnabled, true);
        });

        var input = com.Find("input.bit-tgi-inp");

        Assert.IsFalse(input.HasAttribute("aria-invalid"));

        var form = com.Find("form");
        form.Submit();

        Assert.IsTrue(input.HasAttribute("aria-invalid"));
        Assert.AreEqual("true", input.GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitTagsInputValidationInvalidCssClassTest()
    {
        var model = new BitTagsInputTestModel { Tags = null };

        var com = RenderComponent<BitTagsInputValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, model);
            parameters.Add(p => p.IsEnabled, true);
        });

        var root = com.Find(".bit-tgi");

        Assert.IsFalse(root.ClassList.Contains("bit-inv"));

        var form = com.Find("form");
        form.Submit();

        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitTagsInputValidationValidatesOnAddTest()
    {
        var model = new BitTagsInputTestModel { Tags = null };

        var com = RenderComponent<BitTagsInputValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, model);
            parameters.Add(p => p.IsEnabled, true);
        });

        com.Find("form").Submit();

        Assert.IsTrue(com.Find(".bit-tgi").ClassList.Contains("bit-inv"));

        var input = com.Find("input.bit-tgi-inp");
        input.Input(new ChangeEventArgs { Value = "apple" });
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsFalse(com.Find(".bit-tgi").ClassList.Contains("bit-inv"));
    }

    #endregion
}
