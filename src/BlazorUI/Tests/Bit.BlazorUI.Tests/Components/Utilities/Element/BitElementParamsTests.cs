using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Element;

/// <summary>
/// Covers the BitParams cascade of the Element: what a BitElementParams fills in - its tag, its event modifiers,
/// and every parameter of BitComponentBase - what it leaves alone because the element wrote it for itself, and that
/// the disabled markup of a cascaded Disabled follows the cascaded tag.
/// </summary>
[TestClass]
public class BitElementParamsTests : BunitTestContext
{
    // The content is what an element is written for, so it is written per element and has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitElement.CascadingParameters),
        nameof(BitElement.ChildContent),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitElementParams elementParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { elementParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitElement>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitElement.ChildContent), (RenderFragment)(b => b.AddContent(0, "content")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitElementParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitElement)}", BitElementParams.ParamName);
        Assert.AreEqual("BitParams.BitElement", BitElementParams.ParamName);
    }

    [TestMethod]
    public void BitElementParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitElementParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitElementParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitElementParamsShouldCarryEveryParameterThatIsNotPerElement()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitElement).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                           .Select(p => p.Name)
                                           .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitElementParams).GetProperty(name), $"BitElementParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitElementShouldApplyCascadedElement()
    {
        var component = RenderWithParams(new BitElementParams { Element = "section" });

        Assert.AreEqual("SECTION", component.Find(".bit-elm").TagName);
    }

    [TestMethod]
    public void BitElementShouldApplyCascadedClickModifiers()
    {
        var component = RenderWithParams(new BitElementParams { StopPropagation = true, PreventDefault = true });

        var root = component.Find(".bit-elm");

        StringAssert.Contains(root.OuterHtml, "onclick:stoppropagation");
        StringAssert.Contains(root.OuterHtml, "onclick:preventdefault");
    }

    [TestMethod]
    public void BitElementShouldApplyCascadedNamedEventModifiers()
    {
        var component = RenderWithParams(new BitElementParams
        {
            StopPropagationEvents = ["dblclick", "pointerdown"],
            PreventDefaultEvents = ["contextmenu"],
        });

        var root = component.Find(".bit-elm");

        StringAssert.Contains(root.OuterHtml, "ondblclick:stoppropagation");
        StringAssert.Contains(root.OuterHtml, "onpointerdown:stoppropagation");
        StringAssert.Contains(root.OuterHtml, "oncontextmenu:preventdefault");
    }

    [TestMethod]
    public void BitElementShouldApplyCascadedNoWrapper()
    {
        var component = RenderWithParams(new BitElementParams { NoWrapper = true, Element = "section" });

        Assert.AreEqual(0, component.FindAll(".bit-elm").Count);
        Assert.AreEqual("content", component.Markup);
    }

    [TestMethod]
    public void BitElementShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitElementParams
        {
            AriaLabel = "cascaded label",
            Class = "cascaded",
            Dir = BitDir.Rtl,
            ForceAnimation = true,
            Id = "cascaded-id",
            Style = "color: red;",
            TabIndex = "2",
            Visibility = BitVisibility.Hidden,
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var root = component.Find(".bit-elm");

        Assert.AreEqual("cascaded label", root.GetAttribute("aria-label"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-fam"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("cascaded-id", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "color: red;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("2", root.GetAttribute("tabindex"));
        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void BitElementShouldWriteTheDisabledMarkupOfACascadedDisabledForTheCascadedTag()
    {
        var component = RenderWithParams(new BitElementParams { Element = "button", Disabled = true });

        var root = component.Find(".bit-elm");

        // The tag and the state arrive together from the cascade, and the markup is the one that tag gets.
        Assert.AreEqual("BUTTON", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.HasAttribute("disabled"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
        Assert.IsFalse(root.HasAttribute("tabindex"));
    }

    [TestMethod]
    public void BitElementShouldTakeADisabledTagThatHtmlCannotDisableOutOfTheTabOrder()
    {
        var component = RenderWithParams(new BitElementParams { Element = "span", Disabled = true, TabIndex = "0" });

        var root = component.Find(".bit-elm");

        Assert.IsFalse(root.HasAttribute("disabled"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitElementShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitElementParams
        {
            Element = "article",
            StopPropagation = true,
            PreventDefault = true,
            StopPropagationEvents = ["dblclick"],
            NoWrapper = true,
            Class = "cascaded",
            Dir = BitDir.Rtl,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitElement.Element), "section");
            builder.AddAttribute(2, nameof(BitElement.StopPropagation), false);
            builder.AddAttribute(3, nameof(BitElement.PreventDefault), false);
            builder.AddAttribute(4, nameof(BitElement.StopPropagationEvents), new[] { "pointerdown" });
            builder.AddAttribute(5, nameof(BitElement.NoWrapper), false);
            builder.AddAttribute(6, nameof(BitElement.Class), "own");
            builder.AddAttribute(7, nameof(BitElement.Dir), (BitDir?)BitDir.Ltr);
        });

        var root = component.Find(".bit-elm");

        Assert.AreEqual("SECTION", root.TagName);
        // A modifier the element wrote as false is a decision, so the cascaded true does not turn it on.
        Assert.IsFalse(root.OuterHtml.Contains("onclick:stoppropagation"));
        Assert.IsFalse(root.OuterHtml.Contains("onclick:preventdefault"));
        StringAssert.Contains(root.OuterHtml, "onpointerdown:stoppropagation");
        Assert.IsFalse(root.OuterHtml.Contains("ondblclick:stoppropagation"));
        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("ltr", root.GetAttribute("dir"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitElementShouldGiveBackACascadedElementWhenTheCascadeStopsCarryingIt()
    {
        var component = RenderWithParams(new BitElementParams { Element = "section", Class = "cascaded" });

        var instance = component.FindComponent<BitElement>().Instance;

        Assert.AreEqual("SECTION", component.Find(".bit-elm").TagName);

        // The same number of params objects, so BitParams keeps its child instead of building a new one.
        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitElementParams() }));

        Assert.AreSame(instance, component.FindComponent<BitElement>().Instance);
        Assert.AreEqual("DIV", component.Find(".bit-elm").TagName);
        Assert.IsFalse(component.Find(".bit-elm").ClassList.Contains("cascaded"));
    }

    [TestMethod]
    public void BitElementShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitElementParams());

        component.MarkupMatches(@"<div class=""bit-elm"" id:ignore>content</div>");
    }
}
