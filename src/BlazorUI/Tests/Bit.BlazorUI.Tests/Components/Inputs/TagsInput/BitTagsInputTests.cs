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
    public void BitTagsInputAccentTest(BitColor? accent, string expectedClass)
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Accent, accent);
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
    public void BitTagsInputClassesAndStylesTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Label, "Tags");
            parameters.Add(p => p.Description, "hint");
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
            parameters.Add(p => p.Classes, new BitTagsInputClassStyles
            {
                Root = "custom-root",
                Label = "custom-label",
                Description = "custom-description",
                InputContainer = "custom-container",
                TagsContainer = "custom-tags",
                Tag = "custom-tag",
                TagText = "custom-tag-text",
                DismissButton = "custom-dismiss",
                Input = "custom-input",
                ClearButton = "custom-clear",
            });
            parameters.Add(p => p.Styles, new BitTagsInputClassStyles
            {
                Tag = "color: red",
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
        Assert.IsTrue(com.Find(".bit-tgi-inp").ClassList.Contains("custom-input"));
        Assert.IsTrue(com.Find(".bit-tgi-cbt").ClassList.Contains("custom-clear"));
        Assert.AreEqual("color: red", com.Find(".bit-tgi-tag").GetAttribute("style"));
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
        Assert.IsFalse(com.Find(".bit-tgi-inp").HasAttribute("data-no-add-on-tab") is false);
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
    public async Task BitTagsInputPublicApiIsInertWhenDisabledTest()
    {
        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, new List<string> { "apple" });
        });

        await com.Instance.AddTagAsync("banana");
        await com.Instance.RemoveTagAsync("apple");
        await com.Instance.Clear();

        Assert.AreEqual(1, com.FindAll(".bit-tgi-tag").Count);
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
