using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

/// <summary>
/// Covers the BitParams cascade of the Nav: what a BitNavParams fills in, what it leaves alone because the nav
/// wrote it for itself, and that one non-generic params object reaches a nav of any item type.
/// </summary>
[TestClass]
public class BitNavParamsTests : BunitTestContext
{
    private static List<BitNavItem> GetItems() =>
    [
        new()
        {
            Text = "Fruits",
            Url = "/fruits",
            IconName = "Home",
            ChildItems = [new() { Text = "Apple", Url = "/apple" }]
        },
        new() { Text = "Contact", Url = "/contact" },
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitNavParams navParams, IList<BitNavItem> items, System.Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { navParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNav<BitNavItem>>(0);
                builder.AddAttribute(1, nameof(BitNav<BitNavItem>.Items), items);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitNavParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitNav<object>)}", BitNavParams.ParamName);
        Assert.AreEqual("BitParams.BitNav", BitNavParams.ParamName);
    }

    [TestMethod]
    public void BitNavParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitNavParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitNavParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitNavShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitNavParams
        {
            Accent = BitColor.Success,
            Color = BitColor.Error,
            Size = BitSize.Large,
            FitWidth = true,
            IconOnly = true,
            Class = "cascaded-class",
            AriaLabel = "Cascaded navigation",
        }, GetItems());

        var root = component.Find(".bit-nav");

        Assert.IsTrue(root.ClassList.Contains("bit-nav-asuc"));
        Assert.IsTrue(root.ClassList.Contains("bit-nav-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-nav-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-nav-ftw"));
        Assert.IsTrue(root.ClassList.Contains("bit-nav-ion"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.AreEqual("Cascaded navigation", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitNavDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitNavParams
        {
            Color = BitColor.Error,
            Size = BitSize.Large,
        }, GetItems(), builder =>
        {
            builder.AddAttribute(2, nameof(BitNav<BitNavItem>.Color), (BitColor?)BitColor.Info);
        });

        var root = component.Find(".bit-nav");

        Assert.IsTrue(root.ClassList.Contains("bit-nav-inf"));
        Assert.IsFalse(root.ClassList.Contains("bit-nav-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-nav-lg"), "the parameters it left unset still come from the cascade");
    }

    [TestMethod]
    public void BitNavParamsShouldApplyAllExpandedBeforeTheFirstRender()
    {
        // The initial expansion state is decided while the nav initializes, so the cascade has to be in by then.
        var component = RenderWithParams(new BitNavParams { AllExpanded = true }, GetItems());

        Assert.AreEqual(3, component.FindAll(".bit-nav-ict").Count);
    }

    [TestMethod]
    public void BitNavParamsShouldApplyTheBehaviorParameters()
    {
        var component = RenderWithParams(new BitNavParams
        {
            AllExpanded = true,
            NoCollapse = true,
            RenderType = BitNavRenderType.Grouped,
        }, GetItems());

        Assert.AreEqual(0, component.FindAll(".bit-nav-cbt").Count, "NoCollapse hides the chevrons");
        Assert.AreEqual(2, component.FindAll(".bit-nav-gcb").Count, "every root item renders as a group header");
    }

    [TestMethod]
    public void BitNavParamsShouldApplyTheManualMode()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/contact");

        var component = RenderWithParams(new BitNavParams { Mode = BitNavMode.Manual }, GetItems());

        // In the automatic mode the current URL would have selected the Contact item.
        Assert.AreEqual(0, component.FindAll(".bit-nav-sel").Count);

        component.FindAll(".bit-nav-ict")[1].Click();

        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
    }

    [TestMethod]
    public void BitNavParamsShouldApplyTheMatch()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/contact/team");

        var component = RenderWithParams(new BitNavParams { Match = BitNavMatch.Prefix }, GetItems());

        Assert.AreEqual(1, component.FindAll(".bit-nav-sel").Count);
        Assert.AreEqual("Contact", component.Find(".bit-nav-sel .bit-nav-itx").TextContent);
    }

    [TestMethod]
    public void BitNavParamsShouldNotReselectOnEveryParameterSet()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/contact");

        var selections = 0;

        var component = RenderWithParams(new BitNavParams { Match = BitNavMatch.Exact, Reselectable = true }, GetItems(), builder =>
        {
            builder.AddAttribute(2, nameof(BitNav<BitNavItem>.OnSelectItem), EventCallback.Factory.Create<BitNavItem>(this, _ => selections++));
        });

        var afterFirstRender = selections;

        component.Render();
        component.Render();

        Assert.AreEqual(afterFirstRender, selections);
    }

    [TestMethod]
    public void BitNavParamsShouldApplyClassesAndStyles()
    {
        var component = RenderWithParams(new BitNavParams
        {
            Classes = new() { Root = "cascaded-root", ItemContainer = "cascaded-item" },
            Styles = new() { Root = "margin: 1px;", ItemContainer = "color: red;" },
        }, GetItems());

        var root = component.Find(".bit-nav");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsTrue(component.Find(".bit-nav-ict").ClassList.Contains("cascaded-item"));
        StringAssert.Contains(component.Find(".bit-nav-ict").GetAttribute("style"), "color: red");
    }

    [TestMethod]
    public void BitNavParamsShouldApplyTheToggleAriaLabelsAndChevronIcon()
    {
        var component = RenderWithParams(new BitNavParams
        {
            ExpandAriaLabel = "Open",
            CollapseAriaLabel = "Close",
            ChevronDownIconName = "Add",
        }, GetItems());

        var chevron = component.Find(".bit-nav-cbt");

        Assert.AreEqual("Open", chevron.GetAttribute("aria-label"));
        Assert.IsTrue(chevron.QuerySelector("i")!.ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitNavParamsShouldApplyTheIndentation()
    {
        var component = RenderWithParams(new BitNavParams
        {
            AllExpanded = true,
            IndentValue = 40,
            IndentPadding = 10,
        }, GetItems());

        // The child of the first item is one level deep and has no children: 1 * 40 + 10.
        StringAssert.Contains(component.FindAll(".bit-nav-ict")[1].GetAttribute("style"), "padding-inline-start:50px");
    }

    [TestMethod]
    public void BitNavParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var component = RenderWithParams(new BitNavParams
        {
            Size = BitSize.Small,
            IconOnly = true,
        }, GetItems(), builder =>
        {
            builder.AddAttribute(2, nameof(BitNav<BitNavItem>.Size), (BitSize?)BitSize.Large);
            builder.AddAttribute(3, nameof(BitNav<BitNavItem>.IconOnly), false);
        });

        var root = component.Find(".bit-nav");

        Assert.IsTrue(root.ClassList.Contains("bit-nav-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-nav-ion"));
    }

    [TestMethod]
    public void BitNavParamsShouldReachTheOptionsApiToo()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitNavParams { Size = BitSize.Small, AllExpanded = true } });
            parameters.AddChildContent<BitNav<BitNavOption>>(nav =>
            {
                nav.AddChildContent<BitNavOption>(o =>
                {
                    o.Add(p => p.Text, "Fruits");
                    o.AddChildContent<BitNavOption>(c => c.Add(p => p.Text, "Apple"));
                });
            });
        });

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains("bit-nav-sm"));
        Assert.IsNull(component.Find(".bit-nav-ict").ParentElement!.QuerySelector("ul")!.GetAttribute("style"));
    }

    [TestMethod]
    public void BitNavParamsShouldNotApplyOutsideTheCascade()
    {
        var component = RenderComponent<BitNav<BitNavItem>>(parameters => parameters.Add(p => p.Items, GetItems()));

        Assert.IsTrue(component.Find(".bit-nav").ClassList.Contains("bit-nav-md"));
    }
}
