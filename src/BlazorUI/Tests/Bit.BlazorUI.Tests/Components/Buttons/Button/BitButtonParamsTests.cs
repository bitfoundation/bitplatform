using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.Button;

/// <summary>
/// Covers the BitParams cascade of the Button: what a BitButtonParams fills in, what it leaves alone because the button
/// wrote it for itself, and that what the button derives from its parameters (its element, its classes, its styles, its
/// rel) follows the cascaded values.
/// </summary>
[TestClass]
public class BitButtonParamsTests : BunitTestContext
{
    // What a button is about rather than how it looks - its content, its templates and what it does when it is
    // clicked - is written per button, so it has no place on the params object. The IsLoadingChanged callback is the
    // other half of a binding the page owns.
    private static readonly string[] _notCascaded =
    [
        nameof(BitButton.CascadingParameters),
        nameof(BitButton.ChildContent),
        nameof(BitButton.PrimaryTemplate),
        nameof(BitButton.SecondaryTemplate),
        nameof(BitButton.LoadingTemplate),
        nameof(BitButton.OnClick),
        nameof(BitButton.IsLoadingChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitButtonParams buttonParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { buttonParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitButton>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitButton)}", BitButtonParams.ParamName);
        Assert.AreEqual("BitParams.BitButton", BitButtonParams.ParamName);
    }

    [TestMethod]
    public void BitButtonParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitButtonParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitButtonParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitButtonParamsShouldCarryEveryParameterThatIsNotPerButton()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitButton).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitButtonParams).GetProperty(name), $"BitButtonParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitButtonShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Variant = BitVariant.Outline,
            Title = "Cascaded Title",
            FullWidth = true,
            NoWrap = true,
            Rounded = true,
            FixedColor = true,
        });

        var bitButton = component.Find(".bit-btn");

        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-suc"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-lg"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-otl"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-flw"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-nwr"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-rnd"));
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-fxc"));
        Assert.AreEqual("Cascaded Title", bitButton.GetAttribute("title"));
    }

    [TestMethod]
    public void BitButtonDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Cascaded Title"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitButton.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(2, nameof(BitButton.Title), "Direct Title");
        });

        var bitButton = component.Find(".bit-btn");

        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-err"));
        Assert.IsFalse(bitButton.ClassList.Contains("bit-btn-suc"));
        Assert.AreEqual("Direct Title", bitButton.GetAttribute("title"));

        // the size that was not written by hand still comes from the cascading parameters
        Assert.IsTrue(bitButton.ClassList.Contains("bit-btn-lg"));
    }

    [TestMethod]
    public void BitButtonParamsShouldResolveTheRelFromTheCascadedHrefAndTarget()
    {
        var component = RenderWithParams(new BitButtonParams
        {
            Href = "https://bitplatform.dev",
            Target = "_blank"
        });

        var bitButton = component.Find(".bit-btn");

        Assert.AreEqual("a", bitButton.TagName, ignoreCase: true);
        Assert.AreEqual("https://bitplatform.dev", bitButton.GetAttribute("href"));
        Assert.AreEqual("_blank", bitButton.GetAttribute("target"));
        Assert.AreEqual("noopener", bitButton.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitButtonParamsShouldApplyTheCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitButtonParams
        {
            IconName = "Emoji",
            Classes = new BitButtonClassStyles { Root = "custom-root", Icon = "custom-icon" },
            Styles = new BitButtonClassStyles { Root = "color: red;", Icon = "font-size: 2rem;" }
        });

        var root = component.Find(".bit-btn");
        var icon = component.Find(".bit-btn-icn");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red;");
        Assert.IsTrue(icon.ClassList.Contains("custom-icon"));
        Assert.AreEqual("font-size: 2rem;", icon.GetAttribute("style"));
    }

    [TestMethod]
    public void BitButtonParamsShouldApplyTheCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitButtonParams
        {
            Id = "cascaded-id",
            Class = "cascaded-class",
            Style = "margin: 1px;",
            Dir = BitDir.Rtl,
            Visibility = BitVisibility.Hidden,
            ForceAnimation = true,
            TabIndex = "4",
            AriaLabel = "Cascaded label",
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var root = component.Find(".bit-btn");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-fam"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("4", root.GetAttribute("tabindex"));
        Assert.AreEqual("Cascaded label", root.GetAttribute("aria-label"));
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void BitButtonParamsShouldDisableTheButtonTheWayTheParameterDoes()
    {
        var component = RenderWithParams(new BitButtonParams { Disabled = true });

        var root = component.Find(".bit-btn");

        // AllowDisabledFocus holds by default, so the disabled button stays in the tab order and says so through aria-disabled.
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsFalse(root.HasAttribute("disabled"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));

        var natively = RenderWithParams(new BitButtonParams { Disabled = true, AllowDisabledFocus = false });

        Assert.IsTrue(natively.Find(".bit-btn").HasAttribute("disabled"));
        Assert.AreEqual("-1", natively.Find(".bit-btn").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitButtonParamsUpdateParametersShouldSetEveryPropertyItCarries()
    {
        var classes = new BitButtonClassStyles { Root = "cascaded-root" };
        var styles = new BitButtonClassStyles { Root = "color: red;" };
        var icon = BitIconInfo.Fa("solid share");

        var @params = new BitButtonParams
        {
            AllowDisabledFocus = false,
            AriaDescription = "Test description",
            AriaHidden = true,
            AutoFocus = true,
            AutoLoading = true,
            ButtonType = BitButtonType.Reset,
            Classes = classes,
            Color = BitColor.Warning,
            Download = "report.pdf",
            Draggable = true,
            FixedColor = true,
            Float = true,
            FloatAbsolute = true,
            FloatOffset = "2rem",
            FloatPosition = BitPosition.TopLeft,
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
            LoadingLabelPlacement = BitPlacement.Top,
            NoWrap = true,
            Reclickable = true,
            Rel = BitLinkRels.NoOpener,
            Rounded = true,
            SecondaryText = "secondary",
            Size = BitSize.Small,
            StopPropagation = true,
            Styles = styles,
            Target = "_blank",
            Title = "Test Title",
            Variant = BitVariant.Outline,
            AriaLabel = "Test Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitButton>().Instance;

        Assert.IsFalse(instance.AllowDisabledFocus);
        Assert.AreEqual("Test description", instance.AriaDescription);
        Assert.IsTrue(instance.AriaHidden);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.AreEqual(BitButtonType.Reset, instance.ButtonType);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("report.pdf", instance.Download);
        Assert.IsTrue(instance.Draggable);
        Assert.IsTrue(instance.FixedColor);
        Assert.IsTrue(instance.Float);
        Assert.IsTrue(instance.FloatAbsolute);
        Assert.AreEqual("2rem", instance.FloatOffset);
        Assert.AreEqual(BitPosition.TopLeft, instance.FloatPosition);
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
        Assert.AreEqual(BitPlacement.Top, instance.LoadingLabelPlacement);
        Assert.IsTrue(instance.NoWrap);
        Assert.IsTrue(instance.Reclickable);
        Assert.AreEqual(BitLinkRels.NoOpener, instance.Rel);
        Assert.IsTrue(instance.Rounded);
        Assert.AreEqual("secondary", instance.SecondaryText);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.StopPropagation);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreEqual("_blank", instance.Target);
        Assert.AreEqual("Test Title", instance.Title);
        Assert.AreEqual(BitVariant.Outline, instance.Variant);
        Assert.AreEqual("Test Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitButtonParamsShouldNotOverwriteWhatTheMarkupAlreadySet()
    {
        var @params = new BitButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Params Title"
        };

        var component = RenderComponent<BitButton>(parameters =>
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
    public void BitButtonShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitButtonParams());

        var root = component.Find(".bit-btn");

        // the root, the default variant and the icon-only box a button with no content is - nothing cascaded on top
        CollectionAssert.AreEquivalent(new[] { "bit-btn", "bit-btn-fil", "bit-btn-ntx" }, root.ClassList.ToArray());
        Assert.IsFalse(root.HasAttribute("style"));
        Assert.IsFalse(root.HasAttribute("title"));
        Assert.IsFalse(root.HasAttribute("aria-disabled"));
    }
}
