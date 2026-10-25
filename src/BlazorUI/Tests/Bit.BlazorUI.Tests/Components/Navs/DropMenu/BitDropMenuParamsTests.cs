using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.DropMenu;

/// <summary>
/// Covers what BitDropMenuTests leaves of the BitParams cascade of the drop menu: that the params object carries every
/// parameter that is not per drop menu, that an empty one changes nothing, that a false the drop menu wrote for itself
/// is a choice the cascade leaves alone, and that the cascaded base parameters, sizing and class styles reach the root,
/// the button and the callout rendered beside them.
/// </summary>
[TestClass]
public class BitDropMenuParamsTests : BunitTestContext
{
    // The open state, the content, the label, the events and the id of a scroll container inside the content all belong
    // to one drop menu: a value cascaded to every drop menu under a BitParams would open, fill or wire them all at once.
    private static readonly string[] _notCascaded =
    [
        nameof(BitDropMenu.Body),
        nameof(BitDropMenu.CascadingParameters),
        nameof(BitDropMenu.ChildContent),
        nameof(BitDropMenu.DefaultIsOpen),
        nameof(BitDropMenu.IsOpen),
        nameof(BitDropMenu.IsOpenChanged),
        nameof(BitDropMenu.OnClick),
        nameof(BitDropMenu.OnDismiss),
        nameof(BitDropMenu.OnOpen),
        nameof(BitDropMenu.ScrollContainerId),
        nameof(BitDropMenu.Template),
        nameof(BitDropMenu.Text),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitDropMenuParams dropMenuParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { dropMenuParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitDropMenu>(0);
                builder.AddAttribute(1, nameof(BitDropMenu.Text), "Menu");
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitDropMenu.ChildContent), (RenderFragment)(b => b.AddContent(0, "Body")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitDropMenuParamsShouldCarryEveryParameterThatIsNotPerDropMenu()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitDropMenu).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                            .Select(p => p.Name)
                                            .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                            .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitDropMenuParams).GetProperty(name), $"BitDropMenuParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitDropMenuShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var alone = RenderComponent<BitDropMenu>(parameters =>
        {
            parameters.Add(p => p.Text, "Menu");
            parameters.AddChildContent("Body");
        });

        var cascaded = RenderWithParams(new BitDropMenuParams()).FindComponent<BitDropMenu>();

        // The ids of the two instances are the only thing that may tell their markup apart.
        var expected = WithoutRenderIds(alone.Markup.Replace(alone.Instance.UniqueId, "{id}"));
        var actual = WithoutRenderIds(cascaded.Markup.Replace(cascaded.Instance.UniqueId, "{id}"));

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void BitDropMenuShouldKeepAnExplicitFalseOverACascadedTrue()
    {
        var component = RenderWithParams(new BitDropMenuParams { NoChevron = true, FullWidth = true, Transparent = true }, builder =>
        {
            builder.AddAttribute(2, nameof(BitDropMenu.NoChevron), false);
            builder.AddAttribute(3, nameof(BitDropMenu.FullWidth), false);
        });

        var root = component.Find(".bit-drm");

        // A false the drop menu wrote for itself is a choice, not an absence the cascade may fill in.
        Assert.AreEqual(1, component.FindAll(".bit-drm-chv").Count);
        Assert.IsFalse(root.ClassList.Contains("bit-drm-flw"));
        // What it left unset still comes from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-drm-trn"));
    }

    [TestMethod]
    public void BitDropMenuShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitDropMenuParams
        {
            Id = "cascaded-id",
            Class = "cascaded",
            Style = "margin:1px",
            Dir = BitDir.Rtl,
            Disabled = true,
            AriaLabel = "Open the menu",
        });

        var root = component.Find(".bit-drm");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));

        // The disabled state reaches the native button both ways, and the label names it.
        var button = component.Find(".bit-drm-btn");

        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        Assert.AreEqual("Open the menu", button.GetAttribute("aria-label"));

        // The direction reaches the callout rendered beside the root as well.
        var callout = component.Find(".bit-drm-cal");

        Assert.AreEqual("rtl", callout.GetAttribute("dir"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-rtl"));
    }

    [TestMethod]
    public void BitDropMenuShouldApplyCascadedSizingClassesAndStyles()
    {
        var component = RenderWithParams(new BitDropMenuParams
        {
            IconName = "Home",
            Width = "20rem",
            MinWidth = "10rem",
            MaxWidth = "30rem",
            MaxHeight = "15rem",
            Border = BitColorKind.Tertiary,
            Classes = new() { Button = "c-button", Icon = "c-icon", Text = "c-text", ChevronDown = "c-chevron", Overlay = "c-overlay", Callout = "c-callout" },
            Styles = new() { Button = "--s-button:1", Icon = "--s-icon:1", Text = "--s-text:1", ChevronDown = "--s-chevron:1", Overlay = "--s-overlay:1", Callout = "--s-callout:1" },
        });

        var parts = new (string selector, string cssClass, string style)[]
        {
            (".bit-drm-btn", "c-button", "--s-button:1"),
            (".bit-drm-icn", "c-icon", "--s-icon:1"),
            (".bit-drm-txt", "c-text", "--s-text:1"),
            (".bit-drm-chv", "c-chevron", "--s-chevron:1"),
            (".bit-drm-ovl", "c-overlay", "--s-overlay:1"),
            (".bit-drm-cal", "c-callout", "--s-callout:1"),
        };

        foreach (var (selector, cssClass, style) in parts)
        {
            var element = component.Find(selector);

            Assert.IsTrue(element.ClassList.Contains(cssClass), $"{selector} does not carry {cssClass}.");
            StringAssert.Contains(element.GetAttribute("style"), style, $"{selector} does not carry {style}.");
        }

        // The sizing travels as the public custom properties the stylesheet reads off the callout.
        var callout = component.Find(".bit-drm-cal");
        var calloutStyle = callout.GetAttribute("style")!;

        StringAssert.Contains(calloutStyle, "--bit-DropMenu-callout-width:20rem;");
        StringAssert.Contains(calloutStyle, "--bit-DropMenu-callout-min-width:10rem;");
        StringAssert.Contains(calloutStyle, "--bit-DropMenu-callout-max-width:30rem;");
        StringAssert.Contains(calloutStyle, "--bit-DropMenu-callout-max-height:15rem;");
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-mxh"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-brd"));
        Assert.IsTrue(callout.ClassList.Contains("bit-drm-btr"));
        Assert.IsTrue(component.Find(".bit-drm-icn").ClassList.Contains("bit-icon--Home"));
    }

    // bUnit writes every element reference and event handler as a blazor:* attribute holding an id numbered per
    // render, so two renders of the very same markup only compare equal without them.
    private static string WithoutRenderIds(string markup) => Regex.Replace(markup, @"\s+blazor:[^=\s>]+=""[^""]*""", string.Empty);
}
