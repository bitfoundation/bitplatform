using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Tooltip;

/// <summary>
/// Covers the BitParams cascade of the Tooltip: what a BitTooltipParams fills in, what it leaves alone because the
/// tooltip wrote it for itself, that every plain parameter has a counterpart on the params object and every property
/// of it reaches the tooltip, and that what the tooltip decides from its parameters - its triggers, its relationship,
/// its measurements - follows the cascaded values. The markup-level cases (a group nested inside the cascade) are in
/// BitTooltipTests through BitTooltipCascadingParamsTest.
/// </summary>
[TestClass]
public class BitTooltipParamsTests : BunitTestContext
{
    // What a tooltip says and whether it is on the screen are per tooltip, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTooltip.CascadingParameters),
        nameof(BitTooltip.DefaultIsShown),
        nameof(BitTooltip.IsShown),
        nameof(BitTooltip.Text),
    ];

    private static PointerEventArgs Mouse() => new() { PointerType = "mouse" };

    private IRenderedComponent<BitParams> RenderWithParams(BitTooltipParams tooltipParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { tooltipParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTooltip>(0);
                builder.AddAttribute(1, nameof(BitTooltip.Text), "Tip");
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitTooltip.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<button>Anchor</button>")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTooltipParamsShouldHaveCorrectParamName()
    {
        var @params = new BitTooltipParams();

        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitTooltip)}", BitTooltipParams.ParamName);
        Assert.AreEqual("BitParams.BitTooltip", BitTooltipParams.ParamName);
        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitTooltipParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitTooltipParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks and templates are deliberately left out, and so is what belongs to one tooltip alone.
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var missing = typeof(BitTooltip).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                        .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                        .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                        .Select(p => p.Name)
                                        .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                        .Where(n => typeof(BitTooltipParams).GetProperty(n) is null)
                                        .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }

    [TestMethod]
    public void BitTooltipParamsShouldApplyEveryPropertyItCarries()
    {
        // The other direction: a property added here but forgotten in UpdateParameters is accepted by the cascade
        // and then never reaches the tooltip.
        var tooltipParams = new BitTooltipParams();
        var tooltip = new BitTooltip();

        var properties = typeof(BitTooltipParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                 .Where(p => p.CanWrite)
                                                 .ToArray();

        foreach (var property in properties)
        {
            var target = typeof(BitTooltip).GetProperty(property.Name)!;

            property.SetValue(tooltipParams, SampleValue(property.PropertyType, target.GetValue(tooltip)));
        }

        tooltipParams.UpdateParameters(tooltip);

        foreach (var property in properties)
        {
            var target = typeof(BitTooltip).GetProperty(property.Name)!;

            Assert.AreEqual(property.GetValue(tooltipParams), target.GetValue(tooltip), $"{property.Name} is not applied.");
        }

        // A value unlike the component's default, so the assertion above cannot pass by coincidence.
        static object SampleValue(Type type, object? current)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool)) return current is true ? false : true;
            if (underlying == typeof(int)) return current is 7 ? 8 : 7;
            if (underlying == typeof(string)) return "sample";
            if (underlying == typeof(BitTooltipClassStyles)) return new BitTooltipClassStyles();
            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying).Cast<object>().ToArray();
                return values.First(v => Equals(v, current) is false);
            }

            throw new NotSupportedException($"Add a sample value for {type}.");
        }
    }

    [TestMethod]
    public void BitTooltipShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitTooltipParams
        {
            Color = BitColor.Warning,
            Size = BitSize.Small,
            FullWidth = true,
            NoAnimation = true,
            Interactive = false,
            Offset = 12,
            ArrowSize = 9,
            MaxWidth = "8rem",
            ZIndex = 42,
            Classes = new() { Root = "cascaded-root", TooltipWrapper = "cascaded-wrapper", Tooltip = "cascaded-tooltip", Arrow = "cascaded-arrow" },
            Styles = new() { Root = "margin: 1px;", TooltipWrapper = "color: red;", Tooltip = "color: green;", Arrow = "color: blue;" },
        });

        var root = component.Find(".bit-ttp");

        foreach (var cls in new[] { "bit-ttp-wrn", "bit-ttp-sm", "bit-ttp-flw", "bit-ttp-nan", "cascaded-root" })
        {
            Assert.IsTrue(root.ClassList.Contains(cls), cls);
        }

        Assert.IsFalse(root.ClassList.Contains("bit-ttp-itr"));

        var style = root.GetAttribute("style")!;

        foreach (var declaration in new[] { "margin: 1px;", "--bit-ttp-offset:12px", "--bit-ttp-arrow-size:9px", "--bit-ttp-max-width:8rem", "--bit-ttp-zindex:42" })
        {
            StringAssert.Contains(style, declaration);
        }

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("cascaded-wrapper"));
        Assert.IsTrue(component.Find(".bit-ttp-ctn").ClassList.Contains("cascaded-tooltip"));
        Assert.IsTrue(component.Find(".bit-ttp-arw").ClassList.Contains("cascaded-arrow"));
        Assert.AreEqual("color: red;", component.Find(".bit-ttp-wrp").GetAttribute("style"));
        Assert.AreEqual("color: green;", component.Find(".bit-ttp-ctn").GetAttribute("style"));
        Assert.AreEqual("color: blue;", component.Find(".bit-ttp-arw").GetAttribute("style"));
    }

    [TestMethod]
    public void BitTooltipShouldApplyCascadedPlacementAndAlignment()
    {
        var component = RenderWithParams(new BitTooltipParams { Placement = BitPlacement.End, Alignment = BitPlacement.Start, HideArrow = true });

        var wrapper = component.Find(".bit-ttp-wrp");

        Assert.IsTrue(wrapper.ClassList.Contains("bit-ttp-end"));
        Assert.IsTrue(wrapper.ClassList.Contains("bit-ttp-atp"));
        Assert.AreEqual(0, component.FindAll(".bit-ttp-arw").Count);
    }

    [TestMethod]
    public void BitTooltipShouldApplyCascadedRelationship()
    {
        var component = RenderWithParams(new BitTooltipParams { Relationship = BitTooltipRelationship.Label });

        var root = component.Find(".bit-ttp");

        Assert.AreEqual(component.Find(".bit-ttp-ctn").Id, root.GetAttribute("aria-labelledby"));
        Assert.IsNull(root.GetAttribute("aria-describedby"));

        // The relationship the JS side mirrors onto the anchor is the cascaded one too.
        Assert.AreEqual("aria-labelledby", Context.JSInterop.Invocations["BitBlazorUI.Utils.setupTooltip"][^1].Arguments[2]);
    }

    // A cascaded trigger is read by the handler that decides on it, not only by the markup.
    [TestMethod]
    public void BitTooltipShouldHonourCascadedTriggers()
    {
        var component = RenderWithParams(new BitTooltipParams { ShowOnHover = false, ShowOnFocus = false, ShowOnClick = true });

        var root = component.Find(".bit-ttp");

        root.TriggerEvent("onpointerenter", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onfocusin", new FocusEventArgs());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerup", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldHonourACascadedHideOnClickAndNoDismissOnEscape()
    {
        var component = RenderWithParams(new BitTooltipParams { HideOnClick = true, NoDismissOnEscape = true });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        // Not marked for the JS side to take the key, and the key the .NET side hears is left alone too.
        Assert.IsFalse(component.Find(".bit-ttp-wrp").HasAttribute("data-bit-ttp-esc"));
        component.Find(".bit-ttp").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));

        component.Find(".bit-ttp").TriggerEvent("onpointerup", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldHonourACascadedNoTouch()
    {
        var component = RenderWithParams(new BitTooltipParams { NoTouch = true });

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", new PointerEventArgs { PointerType = "touch" });

        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    // LazyRender is read when the content is first decided on, so the cascade has to reach it before then.
    [TestMethod]
    public void BitTooltipShouldHonourACascadedLazyRender()
    {
        var component = RenderWithParams(new BitTooltipParams { LazyRender = true });

        Assert.AreEqual(string.Empty, component.Find(".bit-ttp-ctn").TextContent);

        component.Find(".bit-ttp").TriggerEvent("onpointerenter", Mouse());

        Assert.AreEqual("Tip", component.Find(".bit-ttp-ctn").TextContent);
    }

    [TestMethod]
    public void BitTooltipShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitTooltipParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded" });

        var root = component.Find(".bit-ttp");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));

        // A cascaded Disabled is read by everything the tooltip decides from it: it describes nothing and shows nothing.
        Assert.IsNull(root.GetAttribute("aria-describedby"));
        root.TriggerEvent("onpointerenter", Mouse());
        Assert.IsFalse(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-vis"));
    }

    [TestMethod]
    public void BitTooltipShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitTooltipParams
        {
            Color = BitColor.Warning,
            Size = BitSize.Small,
            Placement = BitPlacement.End,
            Interactive = false,
            HideArrow = true,
            Offset = 12,
            Relationship = BitTooltipRelationship.Label,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitTooltip.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(3, nameof(BitTooltip.Size), (BitSize?)BitSize.Large);
            builder.AddAttribute(4, nameof(BitTooltip.Placement), BitPlacement.Bottom);
            builder.AddAttribute(5, nameof(BitTooltip.Interactive), true);
            builder.AddAttribute(6, nameof(BitTooltip.HideArrow), false);
            builder.AddAttribute(7, nameof(BitTooltip.Offset), (int?)4);
            builder.AddAttribute(8, nameof(BitTooltip.Relationship), BitTooltipRelationship.Description);
        });

        var root = component.Find(".bit-ttp");

        Assert.IsTrue(root.ClassList.Contains("bit-ttp-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-ttp-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-ttp-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-ttp-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-ttp-itr"));

        var style = root.GetAttribute("style")!;
        StringAssert.Contains(style, "--bit-ttp-offset:4px");
        Assert.IsFalse(style.Contains("--bit-ttp-offset:12px"));

        Assert.AreEqual(component.Find(".bit-ttp-ctn").Id, root.GetAttribute("aria-describedby"));
        Assert.IsNull(root.GetAttribute("aria-labelledby"));

        Assert.IsTrue(component.Find(".bit-ttp-wrp").ClassList.Contains("bit-ttp-btm"));
        Assert.AreEqual(1, component.FindAll(".bit-ttp-arw").Count);
    }

    [TestMethod]
    public void BitTooltipShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitTooltipParams());

        var root = component.Find(".bit-ttp");

        CollectionAssert.AreEquivalent(new[] { "bit-ttp", "bit-ttp-itr" }, root.ClassList.ToArray());
        Assert.IsTrue(string.IsNullOrEmpty(root.GetAttribute("style")));
        Assert.AreEqual("bit-ttp-wrp bit-ttp-top bit-ttp-ahc", component.Find(".bit-ttp-wrp").ClassName.Trim());
        Assert.AreEqual(component.Find(".bit-ttp-ctn").Id, root.GetAttribute("aria-describedby"));
        Assert.AreEqual(1, component.FindAll(".bit-ttp-arw").Count);
        Assert.AreEqual("Tip", component.Find(".bit-ttp-ctn").TextContent);
    }
}
