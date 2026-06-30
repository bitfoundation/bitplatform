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
    }

    [TestMethod]
    public void BitTagsInputDismissButtonNotRenderedWhenReadOnlyTest()
    {
        var initialTags = new List<string> { "apple" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, initialTags);
            parameters.Add(p => p.ReadOnly, true);
        });

        var dismissButtons = com.FindAll(".bit-tgi-dbt");

        Assert.AreEqual(0, dismissButtons.Count);
    }

    [TestMethod]
    public void BitTagsInputDismissButtonNotRenderedWhenDisabledTest()
    {
        var initialTags = new List<string> { "apple" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, initialTags);
            parameters.Add(p => p.IsEnabled, false);
        });

        var dismissButtons = com.FindAll(".bit-tgi-dbt");

        Assert.AreEqual(0, dismissButtons.Count);
    }

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
    }

    [TestMethod]
    public async Task BitTagsInputOnRemoveCallbackTest()
    {
        string? removedTag = null;
        var initialTags = new List<string> { "apple" };

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, initialTags);
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        var dismissBtn = com.Find(".bit-tgi-dbt");
        await dismissBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.AreEqual("apple", removedTag);
    }

    [TestMethod]
    public async Task BitTagsInputDuplicateNotAllowedByDefaultTest()
    {
        var tags = new List<string> { "apple" };
        string? existsTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, tags);
            parameters.Add(p => p.OnTagExists, (string tag) => existsTag = tag);
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual("apple", existsTag);
    }

    [TestMethod]
    public async Task BitTagsInputDuplicateAllowedTest()
    {
        var tags = new List<string> { "apple" };
        string? existsTag = null;
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, tags);
            parameters.Add(p => p.Duplicates, true);
            parameters.Add(p => p.OnTagExists, (string tag) => existsTag = tag);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(addedTags => addedTag = addedTags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "apple" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(existsTag);
        Assert.AreEqual("apple", addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputMaxTagsTest()
    {
        var tags = new List<string> { "apple", "banana" };
        string? addedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, tags);
            parameters.Add(p => p.MaxTags, 2);
            parameters.Add(p => p.OnAdd, (Action<IReadOnlyList<string>>)(addedTags => addedTag = addedTags.FirstOrDefault()));
        });

        var input = com.Find(".bit-tgi-inp");
        await input.InputAsync(new ChangeEventArgs { Value = "cherry" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsNull(addedTag);
    }

    [TestMethod]
    public async Task BitTagsInputMaxLengthTest()
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

        Assert.IsNotNull(addedTag);
        Assert.IsTrue(addedTag.Length <= 5);
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
    }

    [TestMethod]
    public async Task BitTagsInputOnBeforeRemoveCancelTest()
    {
        var tags = new List<string> { "apple" };
        string? removedTag = null;

        var com = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Value, tags);
            parameters.Add(p => p.OnBeforeRemove, (BitTagsInputBeforeArgs args) => args.Cancel = true);
            parameters.Add(p => p.OnRemove, (string tag) => removedTag = tag);
        });

        var dismissBtn = com.Find(".bit-tgi-dbt");
        await dismissBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.IsNull(removedTag);
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
}
