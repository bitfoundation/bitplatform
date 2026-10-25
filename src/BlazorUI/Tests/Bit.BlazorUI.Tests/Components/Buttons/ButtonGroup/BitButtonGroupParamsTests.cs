using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ButtonGroup;

/// <summary>
/// Covers the BitParams cascade of the ButtonGroup: what a BitButtonGroupParams fills in, what it leaves alone because
/// the group wrote it for itself, that the values read while the group initializes (the selection mode, the toggle
/// defaults) arrive in time, and that one cascade reaches the groups of every item type.
/// </summary>
[TestClass]
public class BitButtonGroupParamsTests : BunitTestContext
{
    // What a group holds rather than how it looks - its items, how they are read, its templates, its callbacks and the
    // two-way bound selection - is written per group, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitButtonGroup<BitButtonGroupItem>.CascadingParameters),
        nameof(BitButtonGroup<BitButtonGroupItem>.ChildContent),
        nameof(BitButtonGroup<BitButtonGroupItem>.Items),
        nameof(BitButtonGroup<BitButtonGroupItem>.ItemTemplate),
        nameof(BitButtonGroup<BitButtonGroupItem>.NameSelectors),
        nameof(BitButtonGroup<BitButtonGroupItem>.OnItemClick),
        nameof(BitButtonGroup<BitButtonGroupItem>.OnToggleChange),
        nameof(BitButtonGroup<BitButtonGroupItem>.Options),
        nameof(BitButtonGroup<BitButtonGroupItem>.ToggleKey),
        nameof(BitButtonGroup<BitButtonGroupItem>.ToggleKeyChanged),
        nameof(BitButtonGroup<BitButtonGroupItem>.ToggleKeys),
        nameof(BitButtonGroup<BitButtonGroupItem>.ToggleKeysChanged),
    ];

    private static List<BitButtonGroupItem> BasicItems =>
    [
        new() { Text = "Bold", Key = "bold" },
        new() { Text = "Italic", Key = "italic" }
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitButtonGroupParams groupParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { groupParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButtonGroup<BitButtonGroupItem>>(0);
                builder.AddAttribute(1, nameof(BitButtonGroup<BitButtonGroupItem>.Items), BasicItems);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitButtonGroup<BitButtonGroupItem>)}", BitButtonGroupParams.ParamName);
        Assert.AreEqual("BitParams.BitButtonGroup", BitButtonGroupParams.ParamName);
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitButtonGroupParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitButtonGroupParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldCarryEveryParameterThatIsNotPerGroup()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitButtonGroup<BitButtonGroupItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                    .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                                    .Select(p => p.Name)
                                                                    .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitButtonGroupParams).GetProperty(name), $"BitButtonGroupParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitButtonGroupShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitButtonGroupParams
        {
            Variant = BitVariant.Outline,
            Color = BitColor.Success,
            Size = BitSize.Large,
            Rounded = true,
            Justified = true,
            Vertical = true,
            FullWidth = true,
            Detached = true,
            IconOnly = true,
            Overflow = BitButtonGroupOverflow.Wrap,
            Gap = "1rem",
        });

        var group = component.Find(".bit-btg");

        Assert.IsTrue(group.ClassList.Contains("bit-btg-otl"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-suc"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-lg"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-rnd"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-jst"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-vrt"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-flw"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-dtc"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-ion"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-wrp"));
        StringAssert.Contains(group.GetAttribute("style"), "--bit-ButtonGroup-gap:1rem");
    }

    [TestMethod]
    public void BitButtonGroupDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitButtonGroupParams
        {
            Variant = BitVariant.Outline,
            Color = BitColor.Success,
            Size = BitSize.Large,
            Rounded = true
        }, builder =>
        {
            builder.AddAttribute(10, nameof(BitButtonGroup<BitButtonGroupItem>.Variant), (BitVariant?)BitVariant.Text);
            builder.AddAttribute(11, nameof(BitButtonGroup<BitButtonGroupItem>.Color), (BitColor?)BitColor.Error);
        });

        var group = component.Find(".bit-btg");

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(group.ClassList.Contains("bit-btg-txt"));
        Assert.IsFalse(group.ClassList.Contains("bit-btg-otl"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-err"));
        Assert.IsFalse(group.ClassList.Contains("bit-btg-suc"));

        // What the group left unset is still filled in from the cascade.
        Assert.IsTrue(group.ClassList.Contains("bit-btg-lg"));
        Assert.IsTrue(group.ClassList.Contains("bit-btg-rnd"));
    }

    [TestMethod]
    public void BitButtonGroupParamsUpdateParametersShouldSetAllProperties()
    {
        var classes = new BitButtonGroupClassStyles { Root = "custom-root" };
        var styles = new BitButtonGroupClassStyles { Root = "color: red;" };

        var @params = new BitButtonGroupParams
        {
            AutoFocus = true,
            Classes = classes,
            Color = BitColor.Warning,
            DefaultToggleKey = "italic",
            DefaultToggleKeys = ["bold"],
            Detached = true,
            DisabledInteractive = true,
            FixedToggle = true,
            FullWidth = true,
            Gap = "1rem",
            IconOnly = true,
            Justified = true,
            MaxToggles = 2,
            Navigable = false,
            Overflow = BitButtonGroupOverflow.Scroll,
            Rounded = true,
            SelectOnFocus = false,
            SelectionMode = BitSelectionMode.Multiple,
            ShowSelectionIndicator = true,
            Size = BitSize.Small,
            Styles = styles,
            Toggle = true,
            Variant = BitVariant.Outline,
            Vertical = true,
            AriaLabel = "Test Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitButtonGroup<BitButtonGroupItem>>().Instance;

        Assert.IsTrue(instance.AutoFocus);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("italic", instance.DefaultToggleKey);
        CollectionAssert.AreEqual(new[] { "bold" }, instance.DefaultToggleKeys?.ToArray());
        Assert.IsTrue(instance.Detached);
        Assert.IsTrue(instance.DisabledInteractive);
        Assert.IsTrue(instance.FixedToggle);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreEqual("1rem", instance.Gap);
        Assert.IsTrue(instance.IconOnly);
        Assert.IsTrue(instance.Justified);
        Assert.AreEqual(2, instance.MaxToggles);
        Assert.IsFalse(instance.Navigable);
        Assert.AreEqual(BitButtonGroupOverflow.Scroll, instance.Overflow);
        Assert.IsTrue(instance.Rounded);
        Assert.IsFalse(instance.SelectOnFocus);
        Assert.AreEqual(BitSelectionMode.Multiple, instance.SelectionMode);
        Assert.IsTrue(instance.ShowSelectionIndicator);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreSame(styles, instance.Styles);
        Assert.IsTrue(instance.Toggle);
        Assert.AreEqual(BitVariant.Outline, instance.Variant);
        Assert.IsTrue(instance.Vertical);
        Assert.AreEqual("Test Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitButtonGroupParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitButtonGroupParams
        {
            Variant = BitVariant.Outline,
            Color = BitColor.Success,
            Size = BitSize.Large
        };

        var component = RenderComponent<BitButtonGroup<BitButtonGroupItem>>(parameters =>
        {
            parameters.Add(p => p.Items, BasicItems);
            parameters.Add(p => p.Variant, BitVariant.Text);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        // The values stay as they were, because HasNotBeenSet returns false for each of them.
        Assert.AreEqual(BitVariant.Text, instance.Variant);
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldApplyClassesAndStyles()
    {
        var classes = new BitButtonGroupClassStyles { Root = "custom-root", Button = "custom-button", Text = "custom-text" };
        var styles = new BitButtonGroupClassStyles { Root = "color: red;", Button = "margin: 5px;", Text = "padding: 10px;" };

        var component = RenderWithParams(new BitButtonGroupParams { Classes = classes, Styles = styles });

        var group = component.Find(".bit-btg");
        var button = component.Find("button");
        var text = component.Find(".bit-btg-btx");

        Assert.IsTrue(group.ClassList.Contains("custom-root"));
        Assert.IsTrue(button.ClassList.Contains("custom-button"));
        Assert.IsTrue(text.ClassList.Contains("custom-text"));
        StringAssert.Contains(group.GetAttribute("style"), "color: red;");
        // The button style is joined with the item's own, which is why the trailing semicolon is not part of it.
        StringAssert.Contains(button.GetAttribute("style"), "margin: 5px");
        StringAssert.Contains(text.GetAttribute("style"), "padding: 10px;");
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldApplyBaseParameters()
    {
        var component = RenderWithParams(new BitButtonGroupParams
        {
            AriaLabel = "Base Label",
            Id = "test-id",
            Disabled = true,
            Style = "background: blue;",
            Class = "base-class"
        });

        var group = component.Find(".bit-btg");

        Assert.AreEqual("Base Label", group.GetAttribute("aria-label"));
        Assert.AreEqual("test-id", group.GetAttribute("id"));
        Assert.IsTrue(group.ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", group.GetAttribute("aria-disabled"));
        StringAssert.Contains(group.GetAttribute("style"), "background: blue;");
        Assert.IsTrue(group.ClassList.Contains("base-class"));
        Assert.IsTrue(component.FindAll("button").All(b => b.HasAttribute("disabled")));
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldApplyTheCascadedDirectionVisibilityTabIndexAndAttributes()
    {
        var component = RenderWithParams(new BitButtonGroupParams
        {
            Dir = BitDir.Rtl,
            Visibility = BitVisibility.Collapsed,
            ForceAnimation = true,
            TabIndex = "2",
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var group = component.Find(".bit-btg");

        Assert.AreEqual("rtl", group.GetAttribute("dir"));
        Assert.IsTrue(group.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(group.ClassList.Contains("bit-fam"));
        StringAssert.Contains(group.GetAttribute("style"), "display:none");
        Assert.AreEqual("yes", group.GetAttribute("data-cascaded"));

        // the tab index is the one of the group's single tab stop
        var buttons = component.FindAll("button");
        Assert.AreEqual("2", buttons[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", buttons[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldNotApplyWhenEmpty()
    {
        var component = RenderWithParams(new BitButtonGroupParams(), builder =>
        {
            builder.AddAttribute(10, nameof(BitButtonGroup<BitButtonGroupItem>.Variant), (BitVariant?)BitVariant.Text);
        });

        var group = component.Find(".bit-btg");

        Assert.IsTrue(group.ClassList.Contains("bit-btg-txt"));
        CollectionAssert.AreEquivalent(new[] { "bit-btg", "bit-btg-txt", "bit-btg-nav" }, group.ClassList.ToArray());
    }

    [TestMethod]
    public void BitButtonGroupCascadedSelectionShouldBeAppliedOnInitialization()
    {
        // The selection mode and the toggle defaults are read while the group initializes, which runs before
        // OnParametersSetAsync, so the cascaded values have to be taken there as well.
        var component = RenderWithParams(new BitButtonGroupParams
        {
            SelectionMode = BitSelectionMode.Single,
            DefaultToggleKey = "italic"
        });

        var buttons = component.FindAll("button");

        Assert.AreEqual("radiogroup", component.Find(".bit-btg").GetAttribute("role"));
        Assert.AreEqual("false", buttons[0].GetAttribute("aria-checked"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitButtonGroupCascadedMultipleSelectionShouldApplyTheDefaultToggleKeysAndTheCap()
    {
        var component = RenderWithParams(new BitButtonGroupParams
        {
            SelectionMode = BitSelectionMode.Multiple,
            DefaultToggleKeys = ["bold", "italic"],
            MaxToggles = 1
        });

        var buttons = component.FindAll("button");

        Assert.AreEqual("toolbar", component.Find(".bit-btg").GetAttribute("role"));
        Assert.AreEqual("true", buttons[0].GetAttribute("aria-pressed"));
        // the cap counts for the defaults too: the second key is past it and its button is out of reach
        Assert.AreEqual("false", buttons[1].GetAttribute("aria-pressed"));
        Assert.AreEqual("true", buttons[1].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitButtonGroupParamsShouldReachGroupsOfEveryItemType()
    {
        // The params carry nothing about the item type, so a single cascade covers the groups of every API.
        var paramsList = new List<IBitComponentParams>
        {
            new BitButtonGroupParams { Variant = BitVariant.Outline, Rounded = true }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButtonGroup<BitButtonGroupItem>>(0);
                builder.AddAttribute(1, nameof(BitButtonGroup<BitButtonGroupItem>.Items), BasicItems);
                builder.CloseComponent();

                builder.OpenComponent<BitButtonGroup<KeylessButtonGroupItem>>(2);
                builder.AddAttribute(3, nameof(BitButtonGroup<KeylessButtonGroupItem>.Items),
                                     (IEnumerable<KeylessButtonGroupItem>)new List<KeylessButtonGroupItem> { new() { Text = "Bold" } });
                builder.AddAttribute(4, nameof(BitButtonGroup<KeylessButtonGroupItem>.NameSelectors),
                                     new BitButtonGroupNameSelectors<KeylessButtonGroupItem> { Text = { Selector = i => i.Text } });
                builder.CloseComponent();

                builder.OpenComponent<BitButtonGroup<BitButtonGroupOption>>(5);
                builder.AddAttribute(6, nameof(BitButtonGroup<BitButtonGroupOption>.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitButtonGroupOption>(0);
                    b.AddAttribute(1, nameof(BitButtonGroupOption.Text), "Bold");
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        var groups = component.FindAll(".bit-btg");

        Assert.AreEqual(3, groups.Count);
        Assert.IsTrue(groups.All(g => g.ClassList.Contains("bit-btg-otl") && g.ClassList.Contains("bit-btg-rnd")));
    }

    public class KeylessButtonGroupItem
    {
        public string? Text { get; set; }
    }
}
