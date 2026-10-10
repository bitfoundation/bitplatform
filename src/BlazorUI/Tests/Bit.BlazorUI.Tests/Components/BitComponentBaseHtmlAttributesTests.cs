using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components;

/// <summary>
/// Pins the HtmlAttributes parameter of BitComponentBase when it is passed explicitly rather than filled from the
/// attributes written on the component: its entries are splatted on the root like any of those, and are what the
/// component's own logic reads a splatted attribute from.
/// </summary>
[TestClass]
public class BitComponentBaseHtmlAttributesTests : BunitTestContext
{
    [TestMethod]
    public void AnExplicitHtmlAttributesDictionaryShouldBeSplattedOnTheRoot()
    {
        var component = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["data-test"] = "x", ["title"] = "Hint" });
        });

        var button = component.Find("button");

        Assert.AreEqual("x", button.GetAttribute("data-test"));
        Assert.AreEqual("Hint", button.GetAttribute("title"));
        // The dictionary used to be splatted as one attribute named after the parameter, valued with its ToString().
        Assert.IsFalse(button.HasAttribute("HtmlAttributes"));
    }

    [TestMethod]
    public void TheComponentShouldReadASplattedAttributeFromAnExplicitHtmlAttributesDictionary()
    {
        var component = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, "a");
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["href"] = "https://bitplatform.dev/", ["role"] = "menuitem" });
        });

        var element = component.Find("a");

        // A disabled anchor drops the href it was given and keeps the role the page chose, both of which it can only
        // do by finding them among its splatted attributes.
        Assert.IsFalse(element.HasAttribute("href"));
        Assert.AreEqual("menuitem", element.GetAttribute("role"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void AnAttributeWrittenOnTheComponentShouldWinOverAnEntryOfTheSameName(bool dictionaryFirst)
    {
        var htmlAttributes = new Dictionary<string, object>
        {
            ["data-test"] = "from-dictionary",
            ["title"] = "from-dictionary",
            ["data-other"] = "from-dictionary",
        };

        // Written through the render tree, as the markup would: HtmlAttributes is not a CaptureUnmatchedValues
        // parameter, so bUnit's AddUnmatched cannot feed it. Either order of the two reaches the component.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitButton>(0);
            if (dictionaryFirst) builder.AddAttribute(1, nameof(BitButton.HtmlAttributes), htmlAttributes);
            builder.AddAttribute(2, "data-test", "written");
            // HTML attribute names are case insensitive, and so is the render tree that writes them.
            builder.AddAttribute(3, "Title", "written");
            if (dictionaryFirst is false) builder.AddAttribute(4, nameof(BitButton.HtmlAttributes), htmlAttributes);
            builder.CloseComponent();
        });

        var button = component.Find("button");

        Assert.AreEqual("written", button.GetAttribute("data-test"));
        Assert.AreEqual("written", button.GetAttribute("title"));
        Assert.AreEqual("from-dictionary", button.GetAttribute("data-other"));
    }

    [TestMethod]
    public void AnExplicitHtmlAttributesDictionaryShouldBeLeftAsItWasGiven()
    {
        var htmlAttributes = new Dictionary<string, object> { ["data-test"] = "x" };

        var component = RenderComponent<BitButton>(parameters => parameters.Add(p => p.HtmlAttributes, htmlAttributes));

        component.Render(parameters => parameters.Add(p => p.HtmlAttributes, htmlAttributes));

        // The component clears its own dictionary on every render, so it never takes the one it was given as its own.
        Assert.AreNotSame(htmlAttributes, component.Instance.HtmlAttributes);
        Assert.HasCount(1, htmlAttributes);
        Assert.AreEqual("x", htmlAttributes["data-test"]);
        Assert.AreEqual("x", component.Find("button").GetAttribute("data-test"));
    }

    [TestMethod]
    public void TheEntriesOfADroppedHtmlAttributesDictionaryShouldGo()
    {
        var component = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["data-test"] = "x" });
        });

        component.Render(parameters => parameters.Add(p => p.HtmlAttributes, null!));

        Assert.IsFalse(component.Find("button").HasAttribute("data-test"));
    }

    [TestMethod]
    public void AnExplicitHtmlAttributesDictionaryShouldOutliveAPassThatLeavesItOut()
    {
        var component = RenderComponent<BitButton>(parameters =>
        {
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["data-test"] = "x" });
        });

        // A pass that sets another parameter alone keeps every parameter it does not name, the dictionary included.
        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        var button = component.Find("button");

        Assert.IsTrue(button.ClassList.Contains("bit-dis"));
        Assert.AreEqual("x", button.GetAttribute("data-test"));
    }

    [TestMethod]
    public void AnyDictionaryTypeShouldBeTakenAsTheHtmlAttributesParameter()
    {
        var htmlAttributes = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { ["data-test"] = "x" });

        // What a wrapper forwarding the attributes it captured passes under the name of the parameter.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddAttribute(1, nameof(BitButton.HtmlAttributes), htmlAttributes);
            builder.CloseComponent();
        });

        Assert.AreEqual("x", component.Find("button").GetAttribute("data-test"));
    }

    [TestMethod]
    public void AValueUnderTheNameOfTheParameterThatIsNoDictionaryShouldBeSplattedAsAnAttribute()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddAttribute(1, nameof(BitButton.HtmlAttributes), "text");
            builder.CloseComponent();
        });

        Assert.AreEqual("text", component.Find("button").GetAttribute(nameof(BitButton.HtmlAttributes)));
    }

    [TestMethod]
    public void ADictionaryAssignedFromCodeShouldBeCopiedRatherThanTaken()
    {
        var htmlAttributes = new Dictionary<string, object> { ["data-test"] = "x" };

        var component = RenderComponent<BitButton>();

        // Assigned from outside on purpose: the setter is public, and what it does with the dictionary is under test.
#pragma warning disable BL0005
        component.Instance.HtmlAttributes = htmlAttributes;
#pragma warning restore BL0005

        Assert.AreNotSame(htmlAttributes, component.Instance.HtmlAttributes);
        Assert.AreEqual("x", component.Instance.HtmlAttributes["data-test"]);

        // Every pass clears the component's own dictionary, which leaves the one it was handed as it was.
        component.Render();

        Assert.HasCount(1, htmlAttributes);
        Assert.AreEqual("x", htmlAttributes["data-test"]);
    }

    [TestMethod]
    public void AnExplicitHtmlAttributesDictionaryShouldWinOverTheOneOfAParamsObject()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams
            {
                HtmlAttributes = new() { ["data-shared"] = "cascaded", ["data-cascaded"] = "cascaded" }
            }]);
            builder.AddChildContent<BitButton>(parameters =>
            {
                parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object> { ["data-shared"] = "own" });
            });
        });

        var button = component.Find("button");

        Assert.AreEqual("own", button.GetAttribute("data-shared"));
        Assert.AreEqual("cascaded", button.GetAttribute("data-cascaded"));
    }

    [TestMethod]
    public void TheAttributesOfTheComponentShouldWinOverTheOnesOfAParamsObjectHoweverEitherCasesThem()
    {
        var component = RenderComponent<BitParams>(builder =>
        {
            builder.Add(p => p.Parameters, [new BitButtonParams
            {
                HtmlAttributes = new() { ["data-shared"] = "cascaded", ["title"] = "cascaded" }
            }]);
            builder.AddChildContent(child =>
            {
                child.OpenComponent<BitButton>(0);
                child.AddAttribute(1, nameof(BitButton.HtmlAttributes), new Dictionary<string, object> { ["Data-Shared"] = "own" });
                child.AddAttribute(2, "Title", "written");
                child.CloseComponent();
            });
        });

        var button = component.Find("button");
        var instance = component.FindComponent<BitButton>().Instance;

        Assert.AreEqual("own", button.GetAttribute("data-shared"));
        Assert.AreEqual("written", button.GetAttribute("title"));
        // Neither cascaded spelling reaches the component beside its own, so the component reads what it renders.
        Assert.HasCount(2, instance.HtmlAttributes);
    }
}
