using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

/// <summary>
/// Covers what BitNavBarTests leaves of the BitParams cascade of the navbar: that the params object carries every
/// parameter that is not per navbar, that an empty one changes nothing, that a false the navbar wrote for itself is a
/// choice the cascade leaves alone, and that the cascaded base parameters reach the root and its items.
/// </summary>
[TestClass]
public class BitNavBarParamsTests : BunitTestContext
{
    // What is typed over the item type - the items, the selection, the item template, the name selectors and the
    // callbacks - and the header and footer content belong to one navbar, so they have no place on the params object,
    // which is what lets one non-generic object fit every navbar whatever it is generic over.
    private static readonly string[] _notCascaded =
    [
        nameof(BitNavBar<BitNavBarItem>.CascadingParameters),
        nameof(BitNavBar<BitNavBarItem>.ChildContent),
        nameof(BitNavBar<BitNavBarItem>.DefaultSelectedItem),
        nameof(BitNavBar<BitNavBarItem>.DefaultSelectedKey),
        nameof(BitNavBar<BitNavBarItem>.FooterTemplate),
        nameof(BitNavBar<BitNavBarItem>.HeaderTemplate),
        nameof(BitNavBar<BitNavBarItem>.Items),
        nameof(BitNavBar<BitNavBarItem>.ItemTemplate),
        nameof(BitNavBar<BitNavBarItem>.NameSelectors),
        nameof(BitNavBar<BitNavBarItem>.OnItemClick),
        nameof(BitNavBar<BitNavBarItem>.OnSelectItem),
        nameof(BitNavBar<BitNavBarItem>.Options),
        nameof(BitNavBar<BitNavBarItem>.SelectedItem),
        nameof(BitNavBar<BitNavBarItem>.SelectedItemChanged),
        nameof(BitNavBar<BitNavBarItem>.SelectedKey),
        nameof(BitNavBar<BitNavBarItem>.SelectedKeyChanged),
    ];

    private static List<BitNavBarItem> GetItems() =>
    [
        new() { Text = "Home", IconName = "Home" },
        new() { Text = "Products", IconName = "ProductVariant" },
        new() { Text = "Settings", IconName = "Settings" },
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitNavBarParams navBarParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { navBarParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNavBar<BitNavBarItem>>(0);
                builder.AddAttribute(1, nameof(BitNavBar<BitNavBarItem>.Items), GetItems());
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitNavBarParamsShouldCarryEveryParameterThatIsNotPerNavBar()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitNavBar<BitNavBarItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                         .Select(p => p.Name)
                                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                         .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitNavBarParams).GetProperty(name), $"BitNavBarParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitNavBarShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var alone = RenderComponent<BitNavBar<BitNavBarItem>>(parameters => parameters.Add(p => p.Items, GetItems()));

        var cascaded = RenderWithParams(new BitNavBarParams()).FindComponent<BitNavBar<BitNavBarItem>>();

        // The ids of the two instances are the only thing that may tell their markup apart.
        var expected = WithoutRenderIds(alone.Markup.Replace(alone.Instance.UniqueId, "{id}"));
        var actual = WithoutRenderIds(cascaded.Markup.Replace(cascaded.Instance.UniqueId, "{id}"));

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void BitNavBarShouldKeepAnExplicitFalseOverACascadedTrue()
    {
        var component = RenderWithParams(new BitNavBarParams { SingleTabStop = true, Vertical = true, Scrollable = true }, builder =>
        {
            builder.AddAttribute(2, nameof(BitNavBar<BitNavBarItem>.SingleTabStop), false);
            builder.AddAttribute(3, nameof(BitNavBar<BitNavBarItem>.Vertical), false);
        });

        var root = component.Find(".bit-nbr");

        // A false the navbar wrote for itself is a choice, not an absence the cascade may fill in: every item keeps
        // its own tab stop, and the bar stays horizontal.
        Assert.IsTrue(component.FindAll(".bit-nbr-itm").All(i => i.HasAttribute("tabindex") is false));
        Assert.IsFalse(root.ClassList.Contains("bit-nbr-vrt"));
        // What it left unset still comes from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-nbr-scr"));
    }

    [TestMethod]
    public void BitNavBarShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitNavBarParams
        {
            Id = "cascaded-id",
            Class = "cascaded",
            Style = "margin:1px",
            Dir = BitDir.Rtl,
            Disabled = true,
        });

        var root = component.Find(".bit-nbr");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));

        // A disabled navbar disables everything it renders.
        var items = component.FindAll(".bit-nbr-itm");

        Assert.AreEqual(3, items.Count);
        Assert.IsTrue(items.All(i => i.ClassList.Contains("bit-nbr-dis")));
    }

    [TestMethod]
    public void BitNavBarShouldApplyTheCascadedNewTabHintAndTemplateRenderMode()
    {
        var items = new List<BitNavBarItem>
        {
            new() { Text = "Docs", Url = "https://example.com", Target = "_blank" },
            new() { Text = "Home", Url = "/" },
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitNavBarParams { NewTabHint = "(new window)", ItemTemplateRenderMode = BitNavItemTemplateRenderMode.Replace }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNavBar<BitNavBarItem>>(0);
                builder.AddAttribute(1, nameof(BitNavBar<BitNavBarItem>.Items), items);
                builder.AddAttribute(2, nameof(BitNavBar<BitNavBarItem>.ItemTemplate), (RenderFragment<BitNavBarItem>)(item => b => b.AddMarkupContent(0, $"<span class=\"replaced\">{item.Text}</span>")));
                builder.CloseComponent();
            });
        });

        // The cascaded Replace mode renders the template in place of every item, so no item of the navbar's own is left.
        Assert.AreEqual(2, component.FindAll(".bit-nbr-lit > .replaced").Count);
        Assert.AreEqual(0, component.FindAll(".bit-nbr-itm").Count);
    }

    // bUnit writes every element reference and event handler as a blazor:* attribute holding an id numbered per
    // render, so two renders of the very same markup only compare equal without them.
    private static string WithoutRenderIds(string markup) => Regex.Replace(markup, @"\s+blazor:[^=\s>]+=""[^""]*""", string.Empty);
}
