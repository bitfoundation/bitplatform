using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

/// <summary>
/// Covers the BitParams cascade of the TagsInput: the params object carries every parameter that is not about one
/// field alone, every value it carries reaches the field and its markup, what the field wrote for itself is left
/// alone, and an empty params object changes nothing. The basic cascade and override are pinned beside the field's
/// own tests.
/// </summary>
[TestClass]
public class BitTagsInputParamsTests : BunitTestContext
{
    // What belongs to one field alone is written per field: the templates are markup of the consumer's own, and the
    // value, the form field it is posted as and the handlers that watch it are never shared between two fields.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTagsInput.CascadingParameters),
    ];

    private static bool IsTemplateOrCallback(Type type)
    {
        if (type == typeof(RenderFragment) || type == typeof(EventCallback)) return true;

        if (type.IsGenericType is false) return false;

        var definition = type.GetGenericTypeDefinition();

        return definition == typeof(RenderFragment<>) || definition == typeof(EventCallback<>);
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitTagsInputParams tagsParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { tagsParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTagsInput>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTagsInputParamsShouldCarryEveryParameterThatIsNotPerField()
    {
        var parameters = typeof(BitTagsInput).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Where(p => IsTemplateOrCallback(p.PropertyType) is false)
                                             .Select(p => p.Name)
                                             .Where(n => _notCascaded.Contains(n) is false)
                                             .ToList();

        var missing = parameters.Where(name => typeof(BitTagsInputParams).GetProperty(name) is null).ToList();

        Assert.AreEqual(0, missing.Count, "BitTagsInputParams is missing: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void BitTagsInputParamsShouldApplyEveryValueItCarries()
    {
        var component = RenderWithParams(new BitTagsInputParams
        {
            AllowReorder = true,
            AutoComplete = "email",
            AutoFocus = true,
            BackspaceEditsLastTag = true,
            CancelConfirmKeysOnEmpty = true,
            CanRemoveTag = t => t != "owner",
            ClearButtonAriaLabel = "Cascaded clear",
            ClearButtonIconName = "Delete",
            ClearButtonTitle = "Cascaded clear title",
            ClearOnBlur = true,
            Color = BitColor.Warning,
            Comparison = StringComparison.OrdinalIgnoreCase,
            Description = "Cascaded description",
            DismissAriaLabelFormat = "Drop {0}",
            DismissIconName = "ChromeClose",
            DismissTitle = "Drop",
            Duplicates = true,
            EditableTags = true,
            EditAriaLabelFormat = "Fix {0}",
            EnterKeyHint = BitEnterKeyHint.Next,
            FixedTagAriaDescription = "Stays.",
            InputMode = BitInputMode.Email,
            IsLoading = true,
            Label = "Cascaded label",
            LessTagsText = "Fewer",
            LoadingAriaLabel = "Cascaded loading",
            MaxDisplayedTags = 2,
            MaxLength = 12,
            MaxTags = 5,
            MinLength = 2,
            MoreTagsAriaLabelFormat = "{0} hidden",
            MoreTagsFormat = "and {0}",
            NoBorder = true,
            NoClearOnEscape = true,
            NoTrim = true,
            Pattern = "^[a-z]+$",
            Placeholder = "Cascaded placeholder",
            Prefix = "To:",
            ReorderAriaLabelFormat = "Grab {0}",
            ReorderIconName = "Move",
            ReorderTitle = "Grab",
            Separators = [";"],
            ShowClearButton = true,
            ShowCounter = true,
            Size = BitSize.Large,
            SpellCheck = true,
            Suffix = "cm",
            TagAriaDescription = "Cascaded hint.",
            TagCountAriaDescriptionFormat = "{0} of {1} picked.",
            TagsAriaLabel = "Cascaded list",
            TagsPlaceholder = "Cascaded more",
            TagVariant = BitVariant.Outline,
            Variant = BitVariant.Fill,
            AriaLabel = "Cascaded aria label",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTagsInput.DefaultValue), (ICollection<string>?)new List<string> { "owner", "editor", "reader" });
        });

        var instance = component.FindComponent<BitTagsInput>().Instance;
        var root = component.Find(".bit-tgi");
        var input = component.Find(".bit-tgi-inp");

        Assert.IsTrue(instance.BackspaceEditsLastTag);
        Assert.IsTrue(instance.ClearOnBlur);
        Assert.IsTrue(instance.Duplicates);
        Assert.IsTrue(instance.NoClearOnEscape);
        Assert.AreEqual(StringComparison.OrdinalIgnoreCase, instance.Comparison);
        Assert.AreEqual("^[a-z]+$", instance.Pattern);
        Assert.AreEqual(12, instance.MaxLength);
        Assert.AreEqual(2, instance.MinLength);
        Assert.AreEqual("Cascaded hint.", instance.TagAriaDescription);
        Assert.AreEqual("Fix {0}", instance.EditAriaLabelFormat);
        Assert.AreEqual("Cascaded loading", instance.LoadingAriaLabel);
        Assert.AreEqual("Cascaded clear", instance.ClearButtonAriaLabel);
        Assert.AreEqual("Delete", instance.ClearButtonIconName);
        Assert.AreEqual("Cascaded clear title", instance.ClearButtonTitle);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);

        Assert.IsTrue(root.ClassList.Contains("bit-tgi-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-fil"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-tgo"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-nbd"));

        Assert.AreEqual("email", input.GetAttribute("autocomplete"));
        Assert.AreEqual("email", input.GetAttribute("inputmode"));
        Assert.AreEqual("next", input.GetAttribute("enterkeyhint"));
        Assert.AreEqual("true", input.GetAttribute("spellcheck"));
        Assert.AreEqual("true", input.GetAttribute("data-no-trim"));
        Assert.AreEqual("true", input.GetAttribute("data-cancel-confirm-keys-on-empty"));
        Assert.AreEqual("[\";\"]", input.GetAttribute("data-separators"));
        Assert.AreEqual("Cascaded more", input.GetAttribute("placeholder"));
        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "Blazor._internal.domWrapper.focus"));

        Assert.AreEqual("Cascaded label", component.Find(".bit-tgi-lbl").TextContent.Trim());
        Assert.AreEqual("Cascaded description", component.Find(".bit-tgi-dsc").TextContent.Trim());
        Assert.AreEqual("To:", component.Find(".bit-tgi-pre").TextContent.Trim());
        Assert.AreEqual("cm", component.Find(".bit-tgi-suf").TextContent.Trim());
        Assert.AreEqual("3/5", component.Find(".bit-tgi-cnr").TextContent.Trim());
        Assert.AreEqual("3 of 5 picked.", component.Find(".bit-tgi-cvs").TextContent.Trim());
        Assert.AreEqual("Cascaded list", component.Find(".bit-tgi-tgs").GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded loading", component.Find(".bit-tgi-spn").GetAttribute("aria-label"));

        // Two of the three tags are drawn, the first of which CanRemoveTag pins in place.
        var tags = component.FindAll(".bit-tgi-tag");
        Assert.AreEqual(2, tags.Count);
        Assert.IsTrue(tags[0].ClassList.Contains("bit-tgi-tag-fix"));
        Assert.AreEqual("Stays.", component.Find($"#{tags[0].GetAttribute("aria-describedby")}").TextContent.Trim());
        Assert.AreEqual("Cascaded hint.", component.Find($"#{tags[1].GetAttribute("aria-describedby")}").TextContent.Trim());
        Assert.IsTrue(tags.All(t => t.GetAttribute("draggable") == "true"));

        var toggle = component.Find(".bit-tgi-tgl");
        Assert.AreEqual("and 1", toggle.TextContent.Trim());
        Assert.AreEqual("1 hidden", toggle.GetAttribute("aria-label"));

        var dismiss = component.Find(".bit-tgi-dbt");
        Assert.AreEqual("Drop editor", dismiss.GetAttribute("aria-label"));
        Assert.AreEqual("Drop", dismiss.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-tgi-dsi").ClassList.Contains("bit-icon--ChromeClose"));

        var reorder = component.FindAll(".bit-tgi-rbt")[0];
        Assert.AreEqual("Grab owner", reorder.GetAttribute("aria-label"));
        Assert.AreEqual("Grab", reorder.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-tgi-rdi").ClassList.Contains("bit-icon--Move"));

        var clear = component.Find(".bit-tgi-cbt");
        Assert.AreEqual("Cascaded clear", clear.GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded clear title", clear.GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-tgi-cli").ClassList.Contains("bit-icon--Delete"));

        component.Find(".bit-tgi-tgl").Click();

        Assert.AreEqual("Fewer", component.Find(".bit-tgi-tgl").TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputParamsShouldApplyTheAnnouncementFormatsAndTheRules()
    {
        var invalid = new List<BitTagsInputInvalidArgs>();

        var component = RenderWithParams(new BitTagsInputParams
        {
            AddedAnnouncementFormat = "+{0}",
            AddedManyAnnouncementFormat = "+{0} tags",
            RemovedAnnouncementFormat = "-{0}",
            ClearedAnnouncementFormat = "-{0} tags",
            InvalidAnnouncementFormat = "no {0}: {1}",
            GetInvalidMessage = args => args.Reason == BitTagsInputInvalidReason.Validator ? "vetoed" : null,
            Validator = t => string.Equals(t, "veto", StringComparison.OrdinalIgnoreCase) is false,
            Transformer = t => t.ToUpperInvariant(),
            GetTagName = t => $"[{t}]",
            RestrictToSuggestions = true,
            Suggestions = ["APPLE", "PEAR", "VETO"],
            MaxSuggestions = 2,
            ShowInvalidMessage = true,
            NoInvalidHighlight = true,
            ShowClearButton = true,
            NoBackspaceRemove = true,
            NoAddOnBlur = true,
            NoAddOnTab = true,
            DebounceTime = 0,
            ThrottleTime = 0,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTagsInput.OnInvalid), EventCallback.Factory.Create<BitTagsInputInvalidArgs>(this, invalid.Add));
        });

        var input = component.Find(".bit-tgi-inp");

        Assert.AreEqual(2, component.FindAll("datalist option").Count);
        Assert.AreEqual("true", input.GetAttribute("data-no-add-on-tab"));

        input.Input("apple");
        input.KeyDown("Enter");

        // The transformer, the suggestion spelling and the tag name all come from the cascade.
        Assert.AreEqual("APPLE", component.Find(".bit-tgi-ttx").TextContent.Trim());
        Assert.AreEqual("+[APPLE]", component.Find(".bit-tgi-lvr").TextContent.Trim());

        component.Find(".bit-tgi-inp").Input("veto");
        component.Find(".bit-tgi-inp").KeyDown("Enter");

        Assert.AreEqual(1, invalid.Count);
        Assert.AreEqual(BitTagsInputInvalidReason.Validator, invalid[0].Reason);
        Assert.AreEqual("vetoed", component.Find(".bit-tgi-ivm").TextContent.Trim());
        Assert.IsFalse(component.Find(".bit-tgi").ClassList.Contains("bit-tgi-rjd"));

        component.Find(".bit-tgi-inp").Input("kiwi");
        component.Find(".bit-tgi-inp").KeyDown("Enter");

        Assert.AreEqual("no [KIWI]: It is not one of the suggestions.", component.Find(".bit-tgi-ivm").TextContent.Trim());

        // The keys the cascade turned off stay off.
        component.Find(".bit-tgi-inp").Input(string.Empty);
        component.Find(".bit-tgi-inp").KeyDown("Backspace");

        Assert.AreEqual(1, component.FindAll(".bit-tgi-tag").Count);

        component.Find(".bit-tgi-inp").Input("pear");
        component.Find(".bit-tgi-inp").FocusOut();

        Assert.AreEqual(1, component.FindAll(".bit-tgi-tag").Count);

        component.Find(".bit-tgi-dbt").Click();

        Assert.AreEqual("-[APPLE]", component.Find(".bit-tgi-lvr").TextContent.Trim());
    }

    [TestMethod]
    public void BitTagsInputParamsShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitTagsInputParams
        {
            Label = "Styled",
            ShowCounter = true,
            Classes = new() { Root = "cascaded-root", Label = "cascaded-label", Tag = "cascaded-tag", Input = "cascaded-input", Counter = "cascaded-counter" },
            Styles = new() { Root = "--s: root;", Label = "--s: label;", Tag = "--s: tag;", Input = "--s: input;", Counter = "--s: counter;" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTagsInput.DefaultValue), (ICollection<string>?)new List<string> { "apple" });
        });

        Assert.IsTrue(component.Find(".bit-tgi").ClassList.Contains("cascaded-root"));
        StringAssert.Contains(component.Find(".bit-tgi").GetAttribute("style"), "--s: root;");
        Assert.IsTrue(component.Find(".bit-tgi-lbl").ClassList.Contains("cascaded-label"));
        Assert.AreEqual("--s: label;", component.Find(".bit-tgi-lbl").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-tgi-tag").ClassList.Contains("cascaded-tag"));
        Assert.AreEqual("--s: tag;", component.Find(".bit-tgi-tag").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-tgi-inp").ClassList.Contains("cascaded-input"));
        Assert.AreEqual("--s: input;", component.Find(".bit-tgi-inp").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-tgi-cnr").ClassList.Contains("cascaded-counter"));
        Assert.AreEqual("--s: counter;", component.Find(".bit-tgi-cnr").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTagsInputShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitTagsInputParams
        {
            Variant = BitVariant.Fill,
            TagVariant = BitVariant.Outline,
            Label = "Cascaded label",
            MaxTags = 5,
            ShowCounter = true,
            EditableTags = true,
            NoBorder = true,
            Dir = BitDir.Rtl,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTagsInput.Variant), (BitVariant?)BitVariant.Text);
            builder.AddAttribute(2, nameof(BitTagsInput.TagVariant), (BitVariant?)BitVariant.Text);
            builder.AddAttribute(3, nameof(BitTagsInput.Label), "Own label");
            builder.AddAttribute(4, nameof(BitTagsInput.MaxTags), 2);
            builder.AddAttribute(5, nameof(BitTagsInput.EditableTags), false);
            builder.AddAttribute(6, nameof(BitTagsInput.Dir), (BitDir?)BitDir.Ltr);
            builder.AddAttribute(7, nameof(BitTagsInput.DefaultValue), (ICollection<string>?)new List<string> { "apple" });
        });

        var root = component.Find(".bit-tgi");

        // What the field wrote for itself - a false included - is a choice the cascade does not override.
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-txt"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-tgt"));
        Assert.AreEqual("Own label", component.Find(".bit-tgi-lbl").TextContent.Trim());
        Assert.AreEqual("1/2", component.Find(".bit-tgi-cnr").TextContent.Trim());
        Assert.AreEqual("ltr", root.GetAttribute("dir"));
        Assert.IsFalse(component.FindComponent<BitTagsInput>().Instance.EditableTags);

        // What it left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-tgi-nbd"));
    }

    [TestMethod]
    public void BitTagsInputParamsUpdateParametersShouldNotOverwriteWhatTheFieldSetItself()
    {
        var @params = new BitTagsInputParams
        {
            Placeholder = "Cascaded placeholder",
            Color = BitColor.Success,
            Size = BitSize.Large,
            MaxTags = 5,
            Duplicates = true,
            Separators = [";"],
            Suggestions = ["a"],
            Comparison = StringComparison.OrdinalIgnoreCase,
            TagsAriaLabel = "Cascaded list",
        };

        var component = RenderComponent<BitTagsInput>(parameters =>
        {
            parameters.Add(p => p.Placeholder, "Own placeholder");
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.MaxTags, 2);
            parameters.Add(p => p.Duplicates, false);
            parameters.Add(p => p.Separators, new[] { "," });
            parameters.Add(p => p.Suggestions, new[] { "b" });
            parameters.Add(p => p.Comparison, StringComparison.Ordinal);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual("Own placeholder", instance.Placeholder);
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual(2, instance.MaxTags);
        Assert.IsFalse(instance.Duplicates);
        CollectionAssert.AreEqual(new[] { "," }, instance.Separators!.ToArray());
        CollectionAssert.AreEqual(new[] { "b" }, instance.Suggestions!.ToArray());
        Assert.AreEqual(StringComparison.Ordinal, instance.Comparison);

        // Only what the field did not set itself is taken.
        Assert.AreEqual("Cascaded list", instance.TagsAriaLabel);
    }

    [TestMethod]
    public void BitTagsInputParamsUpdateParametersShouldIgnoreANullField()
    {
        var @params = new BitTagsInputParams { Label = "Cascaded label" };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitTagsInputShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitTagsInputParams());

        var root = component.Find(".bit-tgi");
        var input = component.Find(".bit-tgi-inp");
        var instance = component.FindComponent<BitTagsInput>().Instance;

        // The defaults of the unset Variant and TagVariant are the only classes beside the root's.
        CollectionAssert.AreEquivalent(new[] { "bit-tgi", "bit-tgi-otl", "bit-tgi-tgf" }, root.ClassList.ToArray());
        Assert.IsNull(root.GetAttribute("style"));
        Assert.AreEqual("off", input.GetAttribute("autocomplete"));
        Assert.IsFalse(input.HasAttribute("placeholder"));
        Assert.IsFalse(input.HasAttribute("data-separators"));
        Assert.AreEqual(StringComparison.Ordinal, instance.Comparison);
        Assert.AreEqual(0, instance.MaxTags);
        Assert.AreEqual(0, component.FindAll(".bit-tgi-lbl").Count);
        Assert.AreEqual(0, component.FindAll("datalist").Count);
    }
}
