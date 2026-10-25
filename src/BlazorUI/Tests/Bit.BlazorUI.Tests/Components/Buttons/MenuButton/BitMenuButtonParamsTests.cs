using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

/// <summary>
/// Covers the BitParams cascade of the MenuButton: what a BitMenuButtonParams fills in, what it leaves alone because
/// the menu button wrote it for itself, and that what the menu button derives from its parameters - on its root and on
/// the callout that is a sibling of it - follows the cascaded values.
/// </summary>
[TestClass]
public class BitMenuButtonParamsTests : BunitTestContext
{
    // What a menu button holds rather than how it looks - its items, how they are read, its templates, its callbacks
    // and the two-way bound states - is written per menu button, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitMenuButton<BitMenuButtonItem>.CascadingParameters),
        nameof(BitMenuButton<BitMenuButtonItem>.ChildContent),
        nameof(BitMenuButton<BitMenuButtonItem>.DefaultSelectedItem),
        nameof(BitMenuButton<BitMenuButtonItem>.HeaderTemplate),
        nameof(BitMenuButton<BitMenuButtonItem>.Items),
        nameof(BitMenuButton<BitMenuButtonItem>.ItemTemplate),
        nameof(BitMenuButton<BitMenuButtonItem>.LoadingTemplate),
        nameof(BitMenuButton<BitMenuButtonItem>.NameSelectors),
        nameof(BitMenuButton<BitMenuButtonItem>.OnClick),
        nameof(BitMenuButton<BitMenuButtonItem>.OnChange),
        nameof(BitMenuButton<BitMenuButtonItem>.OnToggleChange),
        nameof(BitMenuButton<BitMenuButtonItem>.Options),
        nameof(BitMenuButton<BitMenuButtonItem>.IsOpen),
        nameof(BitMenuButton<BitMenuButtonItem>.IsOpenChanged),
        nameof(BitMenuButton<BitMenuButtonItem>.IsToggled),
        nameof(BitMenuButton<BitMenuButtonItem>.IsToggledChanged),
        nameof(BitMenuButton<BitMenuButtonItem>.IsLoadingChanged),
        nameof(BitMenuButton<BitMenuButtonItem>.SelectedItem),
        nameof(BitMenuButton<BitMenuButtonItem>.SelectedItemChanged),
    ];

    private static List<BitMenuButtonItem> Items =>
    [
        new() { Text = "Item A", Key = "A" },
        new() { Text = "Item B", Key = "B" }
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitMenuButtonParams menuParams, Action<RenderTreeBuilder>? extraAttributes = null, List<BitMenuButtonItem>? items = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { menuParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMenuButton<BitMenuButtonItem>>(0);
                builder.AddComponentParameter(1, nameof(BitMenuButton<BitMenuButtonItem>.Items), items ?? Items);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitMenuButton<BitMenuButtonItem>)}", BitMenuButtonParams.ParamName);
        Assert.AreEqual("BitParams.BitMenuButton", BitMenuButtonParams.ParamName);
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitMenuButtonParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitMenuButtonParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldCarryEveryParameterThatIsNotPerMenuButton()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitMenuButton<BitMenuButtonItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                  .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                                  .Select(p => p.Name)
                                                                  .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitMenuButtonParams).GetProperty(name), $"BitMenuButtonParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitMenuButtonShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitMenuButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Variant = BitVariant.Outline,
            FullWidth = true,
            Text = "Cascaded Text",
            Title = "Cascaded Title",
            IconName = "Add",
        });

        var menuButton = component.Find(".bit-mnb");

        Assert.IsTrue(menuButton.ClassList.Contains("bit-mnb-suc"));
        Assert.IsTrue(menuButton.ClassList.Contains("bit-mnb-lg"));
        Assert.IsTrue(menuButton.ClassList.Contains("bit-mnb-otl"));
        Assert.IsTrue(menuButton.ClassList.Contains("bit-mnb-flw"));
        Assert.AreEqual("Cascaded Title", component.Find(".bit-mnb-opb").GetAttribute("title"));
        Assert.AreEqual("Cascaded Text", component.Find(".bit-mnb-opb .bit-mnb-btx").TextContent);
        Assert.IsTrue(component.Find(".bit-mnb-opb .bit-mnb-ico").ClassList.Contains("bit-icon--Add"));

        // the callout is a sibling of the root, so it repeats the color and size classes the items read
        var callout = component.Find(".bit-mnb-cal");
        Assert.IsTrue(callout.ClassList.Contains("bit-mnb-suc"));
        Assert.IsTrue(callout.ClassList.Contains("bit-mnb-lg"));
    }

    [TestMethod]
    public void BitMenuButtonDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitMenuButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Text = "Cascaded Text"
        }, builder =>
        {
            builder.AddComponentParameter(10, nameof(BitMenuButton<BitMenuButtonItem>.Color), (BitColor?)BitColor.Error);
            builder.AddComponentParameter(11, nameof(BitMenuButton<BitMenuButtonItem>.Text), "Direct Text");
        });

        var menuButton = component.Find(".bit-mnb");

        Assert.IsTrue(menuButton.ClassList.Contains("bit-mnb-err"));
        Assert.IsFalse(menuButton.ClassList.Contains("bit-mnb-suc"));
        Assert.AreEqual("Direct Text", component.Find(".bit-mnb-opb .bit-mnb-btx").TextContent);

        // the size that was not written by hand still comes from the cascading parameters
        Assert.IsTrue(menuButton.ClassList.Contains("bit-mnb-lg"));
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldCascadeTheNewTabHint()
    {
        var component = RenderWithParams(new BitMenuButtonParams { NewTabHint = "(new window)" },
                                         items: [new() { Text = "Docs", Key = "A", Href = "https://bitplatform.dev", Target = "_blank" }]);

        Assert.AreEqual("(new window)", component.Find("a.bit-mnb-itm .bit-mnb-nth").TextContent);
    }

    [TestMethod]
    public void BitMenuButtonParamsUpdateParametersShouldSetAllProperties()
    {
        var classes = new BitMenuButtonClassStyles { Root = "cascaded-root" };
        var styles = new BitMenuButtonClassStyles { Root = "color: red;" };
        var icon = BitIconInfo.Fa("solid share");
        var checkIcon = BitIconInfo.Fa("solid check");
        var chevronIcon = BitIconInfo.Fa("solid chevron-down");
        var radioIcon = BitIconInfo.Fa("solid circle");
        var submenuIcon = BitIconInfo.Fa("solid chevron-right");

        var @params = new BitMenuButtonParams
        {
            AriaDescription = "Test description",
            AriaHidden = true,
            AutoFocus = true,
            AutoLoading = true,
            Background = BitColorKind.Secondary,
            ButtonType = BitButtonType.Reset,
            CheckIcon = checkIcon,
            CheckIconName = "CheckMark",
            ChevronDownAriaLabel = "More",
            ChevronDownIcon = chevronIcon,
            ChevronDownIconName = "ChevronDown",
            ChevronDownTitle = "Open the menu",
            Classes = classes,
            CloseOnItemClick = false,
            Color = BitColor.Warning,
            DefaultIsToggled = true,
            DisabledInteractive = true,
            DropDirection = BitDropDirection.All,
            FormId = "my-form",
            FullWidth = true,
            Icon = icon,
            IconName = "Share",
            IconOnly = true,
            IsLoading = true,
            LoadingDelay = 300,
            LoadingLabel = "Loading...",
            MaxHeight = "12rem",
            NewTabHint = "(new window)",
            NoIcon = true,
            NoNewTabHint = true,
            RadioIcon = radioIcon,
            RadioIconName = "RadioBtnOn",
            Reclickable = true,
            Size = BitSize.Small,
            Split = true,
            Sticky = true,
            StopPropagation = true,
            Styles = styles,
            SubmenuIcon = submenuIcon,
            SubmenuIconName = "ChevronRightMed",
            Text = "Test Text",
            Title = "Test Title",
            Toggle = true,
            Variant = BitVariant.Text,
            AriaLabel = "Test Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitMenuButton<BitMenuButtonItem>>().Instance;

        Assert.AreEqual("Test description", instance.AriaDescription);
        Assert.IsTrue(instance.AriaHidden);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.AreEqual(BitColorKind.Secondary, instance.Background);
        Assert.AreEqual(BitButtonType.Reset, instance.ButtonType);
        Assert.AreSame(checkIcon, instance.CheckIcon);
        Assert.AreEqual("CheckMark", instance.CheckIconName);
        Assert.AreEqual("More", instance.ChevronDownAriaLabel);
        Assert.AreSame(chevronIcon, instance.ChevronDownIcon);
        Assert.AreEqual("ChevronDown", instance.ChevronDownIconName);
        Assert.AreEqual("Open the menu", instance.ChevronDownTitle);
        Assert.AreSame(classes, instance.Classes);
        Assert.IsFalse(instance.CloseOnItemClick);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.IsTrue(instance.DefaultIsToggled);
        Assert.IsTrue(instance.DisabledInteractive);
        Assert.AreEqual(BitDropDirection.All, instance.DropDirection);
        Assert.AreEqual("my-form", instance.FormId);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreSame(icon, instance.Icon);
        Assert.AreEqual("Share", instance.IconName);
        Assert.IsTrue(instance.IconOnly);
        Assert.IsTrue(instance.IsLoading);
        Assert.AreEqual(300, instance.LoadingDelay);
        Assert.AreEqual("Loading...", instance.LoadingLabel);
        Assert.AreEqual("12rem", instance.MaxHeight);
        Assert.AreEqual("(new window)", instance.NewTabHint);
        Assert.IsTrue(instance.NoIcon);
        Assert.IsTrue(instance.NoNewTabHint);
        Assert.AreSame(radioIcon, instance.RadioIcon);
        Assert.AreEqual("RadioBtnOn", instance.RadioIconName);
        Assert.IsTrue(instance.Reclickable);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.Split);
        Assert.IsTrue(instance.Sticky);
        Assert.IsTrue(instance.StopPropagation);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreSame(submenuIcon, instance.SubmenuIcon);
        Assert.AreEqual("ChevronRightMed", instance.SubmenuIconName);
        Assert.AreEqual("Test Text", instance.Text);
        Assert.AreEqual("Test Title", instance.Title);
        Assert.IsTrue(instance.Toggle);
        Assert.AreEqual(BitVariant.Text, instance.Variant);
        Assert.AreEqual("Test Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitMenuButtonParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitMenuButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Text = "Params Text"
        };

        var component = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Text, "Existing Text");
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Text", instance.Text);
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldApplyClassesAndStyles()
    {
        var classes = new BitMenuButtonClassStyles { Root = "custom-root", OperatorButton = "custom-operator", Callout = "custom-callout" };
        var styles = new BitMenuButtonClassStyles { Root = "color: red;", OperatorButton = "padding: 10px;", Callout = "color: blue;" };

        var component = RenderWithParams(new BitMenuButtonParams { Classes = classes, Styles = styles });

        var menuButton = component.Find(".bit-mnb");
        var operatorButton = component.Find(".bit-mnb-opb");
        var callout = component.Find(".bit-mnb-cal");

        Assert.IsTrue(menuButton.ClassList.Contains("custom-root"));
        StringAssert.Contains(menuButton.GetAttribute("style"), "color: red;");
        Assert.IsTrue(operatorButton.ClassList.Contains("custom-operator"));
        StringAssert.Contains(operatorButton.GetAttribute("style"), "padding: 10px;");
        Assert.IsTrue(callout.ClassList.Contains("custom-callout"));
        StringAssert.Contains(callout.GetAttribute("style"), "color: blue;");
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldApplyBaseParameters()
    {
        var component = RenderWithParams(new BitMenuButtonParams
        {
            AriaLabel = "Base Label",
            Id = "test-id",
            Disabled = true,
            Style = "background: blue;",
            Class = "base-class"
        });

        var menuButton = component.Find(".bit-mnb");
        var operatorButton = component.Find(".bit-mnb-opb");

        Assert.AreEqual("test-id", menuButton.Id);
        Assert.IsTrue(menuButton.ClassList.Contains("bit-dis"));
        Assert.IsTrue(menuButton.ClassList.Contains("base-class"));
        StringAssert.Contains(menuButton.GetAttribute("style"), "background: blue;");

        Assert.AreEqual("Base Label", operatorButton.GetAttribute("aria-label"));
        Assert.IsTrue(operatorButton.HasAttribute("disabled"));
        Assert.AreEqual("true", operatorButton.GetAttribute("aria-disabled"));
        Assert.AreEqual("-1", operatorButton.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldApplyTheCascadedDirectionAndAnimationToTheCalloutToo()
    {
        var component = RenderWithParams(new BitMenuButtonParams
        {
            Dir = BitDir.Rtl,
            ForceAnimation = true,
            Visibility = BitVisibility.Hidden,
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var menuButton = component.Find(".bit-mnb");
        var callout = component.Find(".bit-mnb-cal");

        Assert.AreEqual("rtl", menuButton.GetAttribute("dir"));
        Assert.IsTrue(menuButton.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(menuButton.ClassList.Contains("bit-fam"));
        StringAssert.Contains(menuButton.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("yes", menuButton.GetAttribute("data-cascaded"));

        // the callout is a sibling of the root, so the direction and the animation opt-out are written on it as well
        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-fam"));
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldSeedTheToggledStateThroughDefaultIsToggled()
    {
        // The toggled state itself is left out of the params class on purpose: a split toggle button writes to it on
        // every click, so a cascaded value would be written back over the click on the next render.
        var component = RenderWithParams(new BitMenuButtonParams { Split = true, Toggle = true, DefaultIsToggled = true });

        Assert.IsTrue(component.Find(".bit-mnb").ClassList.Contains("bit-mnb-tgl"));
        Assert.AreEqual("true", component.Find(".bit-mnb-opb").GetAttribute("aria-pressed"));

        component.Find(".bit-mnb-opb").Click();

        Assert.IsFalse(component.Find(".bit-mnb").ClassList.Contains("bit-mnb-tgl"));
    }

    [TestMethod]
    public void BitMenuButtonParamsShouldNotApplyWhenNull()
    {
        var component = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items);
        });

        var instance = component.Instance;

        Assert.IsNull(instance.CascadingParameters);
        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Size);
        Assert.IsNull(instance.Text);
    }

    [TestMethod]
    public void BitMenuButtonShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitMenuButtonParams(), builder =>
        {
            builder.AddComponentParameter(10, nameof(BitMenuButton<BitMenuButtonItem>.Variant), (BitVariant?)BitVariant.Text);
        });

        var menuButton = component.Find(".bit-mnb");

        CollectionAssert.AreEquivalent(new[] { "bit-mnb", "bit-mnb-nsp", "bit-mnb-txt" }, menuButton.ClassList.ToArray());
        Assert.IsFalse(menuButton.HasAttribute("style"));
    }
}
