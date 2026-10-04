using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Covers the BitParams cascade of the ChoiceGroup: what a BitChoiceGroupParams fills in, what it leaves
/// alone because the group wrote it for itself, and that one non-generic params object reaches a group of
/// any item and value types.
/// </summary>
[TestClass]
public class BitChoiceGroupParamsTests : BunitTestContext
{
    private static List<BitChoiceGroupItem<string>> GetItems() =>
    [
        new() { Text = "Item A", Value = "A" },
        new() { Text = "Item B", Value = "B" },
    ];

    private static RenderFragment RenderChoiceGroup(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(0);
            builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Items), GetItems());
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    [TestMethod]
    public void BitChoiceGroupParamsShouldHaveCorrectParamName()
    {
        var paramName = BitChoiceGroupParams.ParamName;
        var expectedName = $"{nameof(BitParams)}.{nameof(BitChoiceGroup<object, object>)}";

        Assert.AreEqual(expectedName, paramName);
        Assert.AreEqual("BitParams.BitChoiceGroup", paramName);
    }

    [TestMethod]
    public void BitChoiceGroupParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitChoiceGroupParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitChoiceGroupParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitChoiceGroupShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitChoiceGroupParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Variant = BitVariant.Outline,
                LabelPosition = BitLabelPosition.Start,
                Horizontal = true,
                FullWidth = true,
                Inline = true,
                NoCircle = true,
                StretchItemLabel = true,
                Label = "Cascaded label",
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderChoiceGroup());
        });

        var root = component.Find(".bit-chg");
        var instance = component.FindComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>().Instance;

        Assert.IsTrue(root.ClassList.Contains("bit-chg-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-lst"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-hor"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-flw"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-inl"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-ncr"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-sil"));
        Assert.AreEqual("Cascaded label", instance.Label);
    }

    [TestMethod]
    public void BitChoiceGroupDirectParametersShouldOverrideCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitChoiceGroupParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Variant = BitVariant.Outline,
                Label = "Cascaded label",
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderChoiceGroup(builder =>
            {
                builder.AddAttribute(2, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Color), BitColor.Error);
                builder.AddAttribute(3, nameof(BitChoiceGroup<BitChoiceGroupItem<string>, string>.Size), BitSize.Small);
            }));
        });

        var root = component.Find(".bit-chg");
        var instance = component.FindComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>().Instance;

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(root.ClassList.Contains("bit-chg-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-sm"));

        // What the group left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-chg-otl"));
        Assert.AreEqual("Cascaded label", instance.Label);
    }

    [TestMethod]
    public void BitChoiceGroupParamsUpdateParametersShouldSetAllProperties()
    {
        var @params = new BitChoiceGroupParams
        {
            AriaLabelledBy = "labelled-by-id",
            AutoReorderOptions = true,
            Color = BitColor.Warning,
            Description = "Cascaded description",
            FullWidth = true,
            Gap = "1rem",
            Horizontal = true,
            Inline = true,
            Label = "Cascaded label",
            LabelPosition = BitLabelPosition.Start,
            NoCircle = true,
            Size = BitSize.Small,
            StretchItemLabel = true,
            Variant = BitVariant.Fill,
            AriaLabel = "Cascaded aria label",
            IsEnabled = false,
            TabIndex = "5",
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(RenderChoiceGroup());
        });

        var instance = component.FindComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>().Instance;

        Assert.AreEqual("labelled-by-id", instance.AriaLabelledBy);
        Assert.IsTrue(instance.AutoReorderOptions);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("Cascaded description", instance.Description);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreEqual("1rem", instance.Gap);
        Assert.IsTrue(instance.Horizontal);
        Assert.IsTrue(instance.Inline);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreEqual(BitLabelPosition.Start, instance.LabelPosition);
        Assert.IsTrue(instance.NoCircle);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.StretchItemLabel);
        Assert.AreEqual(BitVariant.Fill, instance.Variant);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);
        Assert.IsFalse(instance.IsEnabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitChoiceGroupParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitChoiceGroupParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Label = "Params label",
        };

        var component = RenderComponent<BitChoiceGroup<BitChoiceGroupItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, GetItems());
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Label, "Existing label");
        });

        var instance = component.Instance;

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing label", instance.Label);

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing label", instance.Label);
    }

    [TestMethod]
    public void BitChoiceGroupParamsShouldApplyClassesAndStylesAndGap()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitChoiceGroupParams
            {
                Gap = "1rem",
                Classes = new BitChoiceGroupClassStyles { Root = "custom-root", ItemLabel = "custom-item-label" },
                Styles = new BitChoiceGroupClassStyles { Root = "color: red;" },
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderChoiceGroup());
        });

        var root = component.Find(".bit-chg");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red;"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("--bit-chg-gap:1rem"));
        Assert.IsTrue(component.Find(".bit-chg-itl").ClassList.Contains("custom-item-label"));
    }

    [TestMethod]
    public void BitChoiceGroupParamsShouldApplyBaseParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitChoiceGroupParams
            {
                AriaLabel = "Base label",
                Id = "test-id",
                Class = "base-class",
                Style = "background: blue;",
                Dir = BitDir.Rtl,
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderChoiceGroup());
        });

        var root = component.Find(".bit-chg");

        Assert.AreEqual("Base label", root.GetAttribute("aria-label"));
        Assert.AreEqual("test-id", root.GetAttribute("id"));
        Assert.IsTrue(root.ClassList.Contains("base-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("background: blue;"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitChoiceGroupParamsShouldNotApplyWhenTheListIsEmpty()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(RenderChoiceGroup());
        });

        var root = component.Find(".bit-chg");

        Assert.IsTrue(root.ClassList.Contains("bit-chg-pri"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-md"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-txt"));
    }

    [TestMethod]
    public void BitChoiceGroupParamsShouldReachTheOptionsApiToo()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitChoiceGroupParams { Variant = BitVariant.Outline, Color = BitColor.Info }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitChoiceGroup<BitChoiceGroupOption<string>, string>>(0);
                builder.AddAttribute(1, nameof(BitChoiceGroup<BitChoiceGroupOption<string>, string>.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitChoiceGroupOption<string>>(0);
                    b.AddAttribute(1, nameof(BitChoiceGroupOption<string>.Text), "Option A");
                    b.AddAttribute(2, nameof(BitChoiceGroupOption<string>.Value), "A");
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-chg");

        Assert.IsTrue(root.ClassList.Contains("bit-chg-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-chg-inf"));
    }
}
