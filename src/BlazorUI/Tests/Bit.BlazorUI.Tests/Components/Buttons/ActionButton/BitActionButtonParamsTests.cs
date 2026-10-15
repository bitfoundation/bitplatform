using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ActionButton;

/// <summary>
/// Covers the BitParams cascade of the ActionButton: what a BitActionButtonParams fills in, what it leaves alone
/// because the button wrote it for itself, and that what the button derives from its parameters (its element, its
/// classes, its styles and its rel) follows the cascaded values.
/// </summary>
[TestClass]
public class BitActionButtonParamsTests : BunitTestContext
{
    // What a button is about rather than how it looks - its content, its templates and what it does when it is
    // clicked - is written per button, so it has no place on the params object. The IsLoadingChanged callback is the
    // other half of a binding the page owns.
    private static readonly string[] _notCascaded =
    [
        nameof(BitActionButton.CascadingParameters),
        nameof(BitActionButton.Body),
        nameof(BitActionButton.ChildContent),
        nameof(BitActionButton.LoadingTemplate),
        nameof(BitActionButton.OnClick),
        nameof(BitActionButton.IsLoadingChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitActionButtonParams buttonParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { buttonParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitActionButton>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitActionButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitActionButton)}", BitActionButtonParams.ParamName);
        Assert.AreEqual("BitParams.BitActionButton", BitActionButtonParams.ParamName);
    }

    [TestMethod]
    public void BitActionButtonParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitActionButtonParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitActionButtonParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitActionButtonParamsShouldCarryEveryParameterThatIsNotPerButton()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitActionButton).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                .Select(p => p.Name)
                                                .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitActionButtonParams).GetProperty(name), $"BitActionButtonParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitActionButtonShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            IconName = "Add",
            Title = "Cascaded Title",
            FullWidth = true,
            Underlined = true,
            NoWrap = true,
            IconPlacement = BitPlacement.End,
        });

        var button = component.Find(".bit-acb");

        Assert.IsTrue(button.ClassList.Contains("bit-acb-suc"));
        Assert.IsTrue(button.ClassList.Contains("bit-acb-lg"));
        Assert.IsTrue(button.ClassList.Contains("bit-acb-fwi"));
        Assert.IsTrue(button.ClassList.Contains("bit-acb-und"));
        Assert.IsTrue(button.ClassList.Contains("bit-acb-nwr"));
        Assert.IsTrue(button.ClassList.Contains("bit-acb-eni"));
        Assert.AreEqual("Cascaded Title", button.GetAttribute("title"));

        var icon = component.Find(".bit-acb-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitActionButtonDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            IconName = "Add",
            Title = "Cascaded Title"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitActionButton.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(2, nameof(BitActionButton.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitActionButton.Title), "Direct Title");
        });

        var button = component.Find(".bit-acb");

        Assert.IsTrue(button.ClassList.Contains("bit-acb-err"));
        Assert.IsFalse(button.ClassList.Contains("bit-acb-suc"));
        Assert.IsTrue(button.ClassList.Contains("bit-acb-sm"));
        Assert.IsFalse(button.ClassList.Contains("bit-acb-lg"));
        Assert.AreEqual("Direct Title", button.GetAttribute("title"));

        // the icon that was not written by hand still comes from the cascading parameters
        Assert.IsTrue(component.Find(".bit-acb-ico").ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitActionButtonParamsUpdateParametersShouldSetEveryPropertyItCarries()
    {
        var classes = new BitActionButtonClassStyles { Root = "cascaded-root" };
        var styles = new BitActionButtonClassStyles { Root = "color: red;" };
        var icon = BitIconInfo.Fa("solid share");

        var @params = new BitActionButtonParams
        {
            AllowDisabledFocus = true,
            AriaDescription = "Test description",
            AriaHidden = true,
            AutoFocus = true,
            AutoLoading = true,
            ButtonType = BitButtonType.Reset,
            Classes = classes,
            Color = BitColor.Warning,
            Download = "report.pdf",
            FormId = "my-form",
            FullWidth = true,
            Href = "https://bitplatform.dev",
            Icon = icon,
            IconName = "Share",
            IconOnly = true,
            IconPlacement = BitPlacement.End,
            IconUrl = "/images/icon.svg",
            IsLoading = true,
            LoadingDelay = 250,
            LoadingLabel = "Sending...",
            NoWrap = true,
            Reclickable = true,
            Rel = BitLinkRels.NoOpener,
            Size = BitSize.Small,
            StopPropagation = true,
            Styles = styles,
            Target = "_blank",
            Title = "Test Title",
            Underlined = true,
            AriaLabel = "Test Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitActionButton>().Instance;

        Assert.IsTrue(instance.AllowDisabledFocus);
        Assert.AreEqual("Test description", instance.AriaDescription);
        Assert.IsTrue(instance.AriaHidden);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.AreEqual(BitButtonType.Reset, instance.ButtonType);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("report.pdf", instance.Download);
        Assert.AreEqual("my-form", instance.FormId);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreEqual("https://bitplatform.dev", instance.Href);
        Assert.AreSame(icon, instance.Icon);
        Assert.AreEqual("Share", instance.IconName);
        Assert.IsTrue(instance.IconOnly);
        Assert.AreEqual(BitPlacement.End, instance.IconPlacement);
        Assert.AreEqual("/images/icon.svg", instance.IconUrl);
        Assert.IsTrue(instance.IsLoading);
        Assert.AreEqual(250, instance.LoadingDelay);
        Assert.AreEqual("Sending...", instance.LoadingLabel);
        Assert.IsTrue(instance.NoWrap);
        Assert.IsTrue(instance.Reclickable);
        Assert.AreEqual(BitLinkRels.NoOpener, instance.Rel);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.StopPropagation);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreEqual("_blank", instance.Target);
        Assert.AreEqual("Test Title", instance.Title);
        Assert.IsTrue(instance.Underlined);
        Assert.AreEqual("Test Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitActionButtonParamsShouldNotOverwriteWhatTheMarkupAlreadySet()
    {
        var @params = new BitActionButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Params Title"
        };

        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Title, "Existing Title");
        });

        @params.UpdateParameters(component.Instance);

        Assert.AreEqual(BitColor.Error, component.Instance.Color);
        Assert.AreEqual(BitSize.Small, component.Instance.Size);
        Assert.AreEqual("Existing Title", component.Instance.Title);
    }

    [TestMethod]
    public void BitActionButtonParamsShouldApplyClassesAndStyles()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            IconName = "Add",
            Classes = new BitActionButtonClassStyles { Root = "custom-root", Icon = "custom-icon", Content = "custom-content" },
            Styles = new BitActionButtonClassStyles { Root = "color: red;", Icon = "margin: 5px;", Content = "padding: 10px;" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitActionButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Content")));
        });

        var button = component.Find(".bit-acb");
        var icon = component.Find(".bit-acb-ico");
        var content = component.Find(".bit-acb-con");

        Assert.IsTrue(button.ClassList.Contains("custom-root"));
        Assert.IsTrue(icon.ClassList.Contains("custom-icon"));
        Assert.IsTrue(content.ClassList.Contains("custom-content"));
        StringAssert.Contains(button.GetAttribute("style"), "color: red;");
        Assert.AreEqual("margin: 5px;", icon.GetAttribute("style"));
        Assert.AreEqual("padding: 10px;", content.GetAttribute("style"));
    }

    [TestMethod]
    public void BitActionButtonParamsShouldResolveTheRelFromTheCascadedHrefAndRel()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            Href = "https://bitplatform.dev",
            Rel = BitLinkRels.NoOpener | BitLinkRels.NoReferrer
        });

        var anchor = component.Find(".bit-acb");

        Assert.AreEqual("a", anchor.TagName, ignoreCase: true);
        Assert.AreEqual("https://bitplatform.dev", anchor.GetAttribute("href"));
        Assert.AreEqual("noopener noreferrer", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitActionButtonParamsShouldHardenTheCascadedBlankTarget()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            Href = "https://bitplatform.dev",
            Target = "_blank"
        });

        var anchor = component.Find("a.bit-acb");

        Assert.AreEqual("_blank", anchor.GetAttribute("target"));
        Assert.AreEqual("noopener", anchor.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitActionButtonParamsShouldApplyBaseParameters()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            AriaLabel = "Base Label",
            Id = "test-id",
            Disabled = true,
            TabIndex = "3",
            Style = "background: blue;",
            Class = "base-class"
        });

        var button = component.Find(".bit-acb");

        Assert.AreEqual("Base Label", button.GetAttribute("aria-label"));
        Assert.AreEqual("test-id", button.GetAttribute("id"));
        // AllowDisabledFocus is off by default, so the disabled button is natively disabled and out of the tab order.
        Assert.IsTrue(button.ClassList.Contains("bit-dis"));
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.AreEqual("-1", button.GetAttribute("tabindex"));
        StringAssert.Contains(button.GetAttribute("style"), "background: blue;");
        Assert.IsTrue(button.ClassList.Contains("base-class"));
    }

    [TestMethod]
    public void BitActionButtonParamsShouldApplyTheCascadedDirectionVisibilityAndAttributes()
    {
        var component = RenderWithParams(new BitActionButtonParams
        {
            Dir = BitDir.Rtl,
            Visibility = BitVisibility.Hidden,
            ForceAnimation = true,
            TabIndex = "4",
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var button = component.Find(".bit-acb");

        Assert.AreEqual("rtl", button.GetAttribute("dir"));
        Assert.IsTrue(button.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(button.ClassList.Contains("bit-fam"));
        StringAssert.Contains(button.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("4", button.GetAttribute("tabindex"));
        Assert.AreEqual("yes", button.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void BitActionButtonShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitActionButtonParams(), builder =>
        {
            builder.AddAttribute(1, nameof(BitActionButton.Color), (BitColor?)BitColor.Primary);
        });

        var button = component.Find(".bit-acb");

        CollectionAssert.AreEquivalent(new[] { "bit-acb", "bit-acb-pri" }, button.ClassList.ToArray());
        Assert.IsFalse(button.HasAttribute("style"));
        Assert.IsFalse(button.HasAttribute("title"));
    }
}
