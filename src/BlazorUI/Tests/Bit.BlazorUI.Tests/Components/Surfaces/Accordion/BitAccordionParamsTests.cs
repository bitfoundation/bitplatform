using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Accordion;

/// <summary>
/// Covers the BitParams cascade of the Accordion: that every parameter which is not about one accordion has a
/// counterpart on BitAccordionParams, that each of them reaches the accordion - its classes, its inline variables,
/// its parts and the icons it swaps - that a value the accordion wrote for itself is kept, that a cascaded value goes
/// back to the default once the cascade stops carrying it, and that an empty params object leaves the accordion as it
/// was. BitAccordionCascadingParamsTest renders the look-and-feel properties and the nested heading level; what is
/// here is what that markup cannot show.
/// </summary>
[TestClass]
public class BitAccordionParamsTests : BunitTestContext
{
    // What an accordion is about rather than how every accordion looks - its header and its content, its state, the
    // icon beside its title and what it answers - is written per accordion, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAccordion.CascadingParameters),
        nameof(BitAccordion.Actions),
        nameof(BitAccordion.Body),
        nameof(BitAccordion.Busy),
        nameof(BitAccordion.ChildContent),
        nameof(BitAccordion.DefaultIsExpanded),
        nameof(BitAccordion.Description),
        nameof(BitAccordion.ExpanderTemplate),
        nameof(BitAccordion.HeaderAriaLabel),
        nameof(BitAccordion.HeaderTemplate),
        nameof(BitAccordion.Icon),
        nameof(BitAccordion.IconName),
        nameof(BitAccordion.IsExpanded),
        nameof(BitAccordion.OnChange),
        nameof(BitAccordion.OnClick),
        nameof(BitAccordion.OnCollapse),
        nameof(BitAccordion.OnExpand),
        nameof(BitAccordion.OnToggling),
        nameof(BitAccordion.ReadOnly),
        nameof(BitAccordion.Title),
        nameof(BitAccordion.TitleTemplate),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAccordionParams accordionParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { accordionParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAccordion>(0);
                builder.AddAttribute(1, nameof(BitAccordion.Title), "Title");
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitAccordion.ChildContent), (RenderFragment)(b => b.AddContent(0, "Body")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAccordionParamsShouldHaveCorrectParamName()
    {
        var @params = new BitAccordionParams();

        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitAccordion)}", BitAccordionParams.ParamName);
        Assert.AreEqual("BitParams.BitAccordion", BitAccordionParams.ParamName);
        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitAccordionParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitAccordionParamsShouldCarryEveryParameterThatIsNotPerAccordion()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAccordion).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false
                                                      && _notCascaded.Contains(n) is false
                                                      && n.EndsWith("Changed", StringComparison.Ordinal) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAccordionParams).GetProperty(name), $"BitAccordionParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAccordionParamsShouldNotCascadeWhatBelongsToOneAccordion()
    {
        foreach (var name in _notCascaded.Where(n => n != nameof(BitAccordion.CascadingParameters)))
        {
            Assert.IsNull(typeof(BitAccordionParams).GetProperty(name), $"BitAccordionParams cascades {name}, which belongs to one accordion.");
        }
    }

    [TestMethod]
    public void BitAccordionShouldSwapTheCascadedExpanderIcons()
    {
        var component = RenderWithParams(new BitAccordionParams
        {
            ExpanderIcon = BitIconInfo.Css("fa-solid fa-plus"),
            ExpandedExpanderIcon = BitIconInfo.Css("fa-solid fa-minus"),
        });

        Assert.IsTrue(component.Find(".bit-acd-eic").ClassList.Contains("fa-plus"));

        component.Find(".bit-acd-hdr").Click();

        // A swapped icon already tells the state, so the wrapper is not turned over as well.
        Assert.IsTrue(component.Find(".bit-acd-eic").ClassList.Contains("fa-minus"));
        Assert.IsFalse(component.Find(".bit-acd-eic").ClassList.Contains("fa-plus"));
        Assert.IsFalse(component.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionOwnExpanderIconNameShouldNotBeReplacedByACascadedExpanderIcon()
    {
        // ExpanderIcon wins over ExpanderIconName, so the pair is one setting: a cascaded icon filled in beside the
        // accordion's own name would replace the chevron it asked for.
        var component = RenderWithParams(new BitAccordionParams { ExpanderIcon = BitIconInfo.Css("fa-solid fa-plus") }, builder =>
        {
            builder.AddAttribute(2, nameof(BitAccordion.ExpanderIconName), "ChevronDown");
        });

        var icon = component.Find(".bit-acd-eic");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--ChevronDown"));
        Assert.IsFalse(icon.ClassList.Contains("fa-plus"));
    }

    [TestMethod]
    public void BitAccordionShouldKeepTheCascadedExpanderStillWithNoExpanderRotation()
    {
        var component = RenderWithParams(new BitAccordionParams { NoExpanderRotation = true }, builder =>
        {
            builder.AddAttribute(2, nameof(BitAccordion.DefaultIsExpanded), true);
        });

        Assert.IsTrue(component.Find(".bit-acd").ClassList.Contains("bit-acd-exp"));
        Assert.IsFalse(component.Find(".bit-acd-eiw").ClassList.Contains("bit-ico--r180"));
    }

    [TestMethod]
    public void BitAccordionShouldHideTheExpanderThroughTheCascade()
    {
        var component = RenderWithParams(new BitAccordionParams { HideExpanderIcon = true });

        Assert.AreEqual(0, component.FindAll(".bit-acd-eiw").Count);
    }

    [TestMethod]
    public void BitAccordionShouldApplyCascadedClassesAndStylesToTheParts()
    {
        var component = RenderWithParams(new BitAccordionParams
        {
            Classes = new()
            {
                Root = "cascaded-root",
                Header = "cascaded-header",
                Title = "cascaded-title",
                ExpanderIcon = "cascaded-expander",
                Content = "cascaded-content",
            },
            Styles = new()
            {
                Root = "margin: 1px;",
                Header = "color: red;",
                Title = "color: blue;",
                Content = "color: green;",
            },
        });

        Assert.IsTrue(component.Find(".bit-acd").ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-acd-hdr").ClassList.Contains("cascaded-header"));
        Assert.IsTrue(component.Find(".bit-acd-ttl").ClassList.Contains("cascaded-title"));
        Assert.IsTrue(component.Find(".bit-acd-eic").ClassList.Contains("cascaded-expander"));
        Assert.IsTrue(component.Find(".bit-acd-con").ClassList.Contains("cascaded-content"));

        StringAssert.Contains(component.Find(".bit-acd").GetAttribute("style"), "margin: 1px;");
        Assert.AreEqual("color: red;", component.Find(".bit-acd-hdr").GetAttribute("style"));
        Assert.AreEqual("color: blue;", component.Find(".bit-acd-ttl").GetAttribute("style"));
        Assert.AreEqual("color: green;", component.Find(".bit-acd-con").GetAttribute("style"));
    }

    [TestMethod]
    public void BitAccordionShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitAccordionParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 2px;",
            AriaLabel = "Cascaded",
        });

        var root = component.Find(".bit-acd");
        var header = component.Find(".bit-acd-hdr");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 2px;");
        Assert.AreEqual("Cascaded", root.GetAttribute("aria-label"));
        Assert.AreEqual("group", root.GetAttribute("role"));

        // A disabled header is one whoever disabled it: out of the tab order, and reported as such.
        Assert.AreEqual("true", header.GetAttribute("aria-disabled"));
        Assert.AreEqual("-1", header.GetAttribute("tabindex"));

        header.Click();

        Assert.IsFalse(component.FindComponent<BitAccordion>().Instance.IsExpanded);
    }

    [TestMethod]
    public void BitAccordionShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAccordionParams
        {
            NoBorder = true,
            TransitionDuration = 150,
            MaxHeight = "10rem",
            ExpandOnPrint = true,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitAccordion.NoBorder), false);
            builder.AddAttribute(3, nameof(BitAccordion.TransitionDuration), 50);
        });

        var root = component.Find(".bit-acd");
        var style = root.GetAttribute("style")!;

        // NoBorder written as false by the accordion opts it out of the cascaded one; what it did not write is
        // still filled in from the cascade.
        Assert.IsFalse(root.ClassList.Contains("bit-acd-nbd"));
        StringAssert.Contains(style, "--bit-acd-dur-full:50ms");
        Assert.IsFalse(style.Contains("--bit-acd-dur-full:150ms"));
        StringAssert.Contains(style, "--bit-acd-max-h:10rem");
        Assert.IsTrue(root.ClassList.Contains("bit-acd-eop"));
    }

    [TestMethod]
    public void BitAccordionShouldGoBackToItsDefaultsOnceTheCascadeStopsCarryingAValue()
    {
        var component = RenderWithParams(new BitAccordionParams { NoBorder = true, Size = BitSize.Large, MaxHeight = "10rem" });

        var root = component.Find(".bit-acd");

        Assert.IsTrue(root.ClassList.Contains("bit-acd-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-acd-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-acd-mxh"));

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitAccordionParams() }));

        var accordion = component.FindComponent<BitAccordion>().Instance;

        Assert.IsFalse(accordion.NoBorder);
        Assert.IsNull(accordion.Size);
        Assert.IsNull(accordion.MaxHeight);

        root = component.Find(".bit-acd");

        Assert.IsFalse(root.ClassList.Contains("bit-acd-nbd"));
        Assert.IsFalse(root.ClassList.Contains("bit-acd-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-acd-mxh"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("--bit-acd-max-h"));
    }

    [TestMethod]
    public void BitAccordionShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitAccordionParams());

        var accordion = component.FindComponent<BitAccordion>().Instance;
        var root = component.Find(".bit-acd");

        Assert.IsNull(accordion.Background);
        Assert.IsNull(accordion.Border);
        Assert.IsNull(accordion.Size);
        Assert.IsNull(accordion.MaxHeight);
        Assert.IsNull(accordion.TransitionDuration);
        Assert.IsNull(accordion.HeadingLevel);
        Assert.IsNull(accordion.ExpanderIcon);
        Assert.IsNull(accordion.ExpanderIconName);
        Assert.IsFalse(accordion.NoBorder);
        Assert.IsFalse(accordion.HiddenUntilFound);
        Assert.IsFalse(accordion.LazyContent);
        Assert.IsFalse(accordion.UnmountOnCollapse);

        Assert.AreEqual(1, root.ClassList.Length);
        Assert.IsTrue(root.ClassList.Contains("bit-acd"));
        Assert.AreEqual("3", component.Find(".bit-acd-hed").GetAttribute("aria-level"));
        Assert.IsTrue(component.Find(".bit-acd-eic").ClassList.Contains("bit-icon--ChevronRight"));
        Assert.AreEqual("region", component.Find(".bit-acd-con").GetAttribute("role"));
    }
}
