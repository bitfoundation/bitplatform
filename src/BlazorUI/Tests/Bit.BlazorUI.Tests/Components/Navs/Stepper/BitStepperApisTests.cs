using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Stepper;

/// <summary>
/// The two other ways a BitStepper is given its steps, besides BitStepperItem objects: objects of a custom class read
/// through NameSelectors (by name or by selector), and BitStepperOption components declared in markup.
/// </summary>
[TestClass]
public class BitStepperApisTests : BunitTestContext
{
    private const string ChildrenAttributes = "BitBlazorUI.Utils.getChildrenAttributes";

    public class Phase
    {
        public string? Name { get; set; }
        public string? Hint { get; set; }
        public string? Glyph { get; set; }
        public bool Failed { get; set; }
        public bool Locked { get; set; }
        public string? Id { get; set; }
        public string? Css { get; set; }
        public string? Inline { get; set; }
        public string? Tooltip { get; set; }
        public string? Label { get; set; }
        public System.Action<Phase>? Clicked { get; set; }
    }

    private static readonly BitStepperNameSelectors<Phase> ByName = new()
    {
        Text = { Name = nameof(Phase.Name) },
        Description = { Name = nameof(Phase.Hint) },
        IconName = { Name = nameof(Phase.Glyph) },
        IsError = { Name = nameof(Phase.Failed) },
        IsDisabled = { Name = nameof(Phase.Locked) },
        Key = { Name = nameof(Phase.Id) },
        Class = { Name = nameof(Phase.Css) },
        Style = { Name = nameof(Phase.Inline) },
        Title = { Name = nameof(Phase.Tooltip) },
        AriaLabel = { Name = nameof(Phase.Label) },
        OnClick = { Name = nameof(Phase.Clicked) },
    };

    private static List<Phase> Phases(List<string?>? clicks = null) =>
    [
        new() { Name = "Plan", Hint = "What to build", Glyph = "Lightbulb", Id = "plan", Css = "plan-class", Inline = "margin:1px", Tooltip = "Planning" },
        new() { Name = "Build", Failed = true, Label = "Build, failed", Clicked = p => clicks?.Add(p.Name) },
        new() { Name = "Ship", Locked = true }
    ];

    private static string[] Texts<T>(IRenderedComponent<T> com) where T : Microsoft.AspNetCore.Components.IComponent => com.FindAll(".bit-str-txt").Select(t => t.TextContent).ToArray();

    [TestMethod]
    public void BitStepperShouldReadTheStepsOfACustomClassByTheNamesOfItsMembers()
    {
        var com = RenderComponent<BitStepper<Phase>>(parameters =>
        {
            parameters.Add(p => p.Items, Phases());
            parameters.Add(p => p.NameSelectors, ByName);
            parameters.Add(p => p.CurrentIndex, 1);
        });

        CollectionAssert.AreEqual(new[] { "Plan", "Build", "Ship" }, Texts(com));

        var steps = com.FindAll("li.bit-str-stp");
        var buttons = com.FindAll(".bit-str-btn");

        Assert.AreEqual("What to build", steps[0].QuerySelector(".bit-str-dsc")!.TextContent);
        Assert.IsTrue(steps[0].ClassList.Contains("plan-class"));
        Assert.AreEqual("margin:1px", steps[0].GetAttribute("style"));
        Assert.AreEqual("Planning", buttons[0].GetAttribute("title"));

        // The completed step shows the check mark over its own icon; the error and the disabled states are read too.
        Assert.IsTrue(steps[0].ClassList.Contains("bit-str-cmp"));
        Assert.IsTrue(steps[1].ClassList.Contains("bit-str-err"));
        Assert.AreEqual("Build, failed", buttons[1].GetAttribute("aria-label"));
        Assert.IsTrue(steps[2].ClassList.Contains("bit-str-sdi"));
        Assert.IsTrue(buttons[2].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitStepperShouldShowTheIconOfACustomStepByName()
    {
        var com = RenderComponent<BitStepper<Phase>>(parameters =>
        {
            parameters.Add(p => p.Items, Phases());
            parameters.Add(p => p.NameSelectors, ByName);
            parameters.Add(p => p.CurrentIndex, 0);
        });

        Assert.IsTrue(com.Find(".bit-str-ico").ClassList.Contains("bit-icon--Lightbulb"));
    }

    [TestMethod]
    public void BitStepperShouldReadTheStepsOfACustomClassBySelectors()
    {
        var selectors = new BitStepperNameSelectors<Phase>
        {
            Text = { Selector = p => p.Name?.ToUpperInvariant() },
            Description = { Selector = p => p.Hint },
            IsError = { Selector = p => p.Failed },
            IsDisabled = { Selector = p => p.Locked },
        };

        var com = RenderComponent<BitStepper<Phase>>(parameters =>
        {
            parameters.Add(p => p.Items, Phases());
            parameters.Add(p => p.NameSelectors, selectors);
        });

        CollectionAssert.AreEqual(new[] { "PLAN", "BUILD", "SHIP" }, Texts(com));
        Assert.AreEqual("What to build", com.Find(".bit-str-dsc").TextContent);
        Assert.IsTrue(com.FindAll("li.bit-str-stp")[1].ClassList.Contains("bit-str-err"));
        Assert.IsTrue(com.FindAll("li.bit-str-stp")[2].ClassList.Contains("bit-str-sdi"));
    }

    [TestMethod]
    public void BitStepperShouldRunTheClickOfACustomStepAndPassTheItem()
    {
        var own = new List<string?>();
        var clicked = new List<string?>();

        var com = RenderComponent<BitStepper<Phase>>(parameters =>
        {
            parameters.Add(p => p.Items, Phases(own));
            parameters.Add(p => p.NameSelectors, ByName);
            parameters.Add(p => p.OnItemClick, (Phase p) => clicked.Add(p.Name));
        });

        com.FindAll(".bit-str-btn")[1].Click();

        CollectionAssert.AreEqual(new[] { "Build" }, own);
        CollectionAssert.AreEqual(new[] { "Build" }, clicked);
        Assert.AreEqual(1, com.Instance.CurrentIndex);

        // The clicks of the selector are run too.
        var selectorClicks = 0;
        var selectors = new BitStepperNameSelectors<Phase> { Text = { Name = nameof(Phase.Name) }, OnClick = { Selector = _ => _ => selectorClicks++ } };

        com.Render(parameters => parameters.Add(p => p.NameSelectors, selectors));
        com.FindAll(".bit-str-btn")[0].Click();

        Assert.AreEqual(1, selectorClicks);
    }

    [TestMethod]
    public void BitStepperShouldReadNothingOfACustomClassWithoutNameSelectors()
    {
        var com = RenderComponent<BitStepper<Phase>>(parameters => parameters.Add(p => p.Items, Phases()));

        // The steps are there, numbered, but with nothing to read their text by.
        Assert.AreEqual(3, com.FindAll("li.bit-str-stp").Count);
        Assert.AreEqual(0, com.FindAll(".bit-str-txt").Count);
        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, com.FindAll(".bit-str-num").Select(n => n.TextContent).ToArray());
    }

    [TestMethod]
    public void BitStepperShouldDrawTheStepsOfItsOptions()
    {
        var com = RenderComponent<BitStepperOptionsTest>(parameters => parameters.Add(p => p.CurrentIndex, 1));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, Texts(com));

        var steps = com.FindAll("li.bit-str-stp");

        Assert.AreEqual("Start here", steps[0].QuerySelector(".bit-str-dsc")!.TextContent);
        Assert.IsTrue(steps[0].ClassList.Contains("bit-str-cmp"));
        Assert.IsTrue(steps[1].ClassList.Contains("bit-str-cur"));
        Assert.IsTrue(steps[1].ClassList.Contains("last-class"));
        Assert.AreEqual("margin:1px", steps[1].GetAttribute("style"));
        Assert.AreEqual("The end", steps[1].QuerySelector(".bit-str-btn")!.GetAttribute("title"));
        Assert.AreEqual(1, com.FindAll(".bit-str-con").Count);

        // The options render a marker each, in an item of the list hidden from everyone, ahead of the steps.
        var holder = com.Find("ol.bit-str > li:first-child");

        Assert.IsTrue(holder.HasAttribute("hidden"));
        Assert.AreEqual("true", holder.GetAttribute("aria-hidden"));
        Assert.AreEqual(2, holder.QuerySelectorAll("[data-bit-str-opt]").Length);
        Assert.AreEqual(0, holder.QuerySelectorAll(".bit-str-stp").Length);
    }

    [TestMethod]
    public void BitStepperShouldRunTheClickOfAnOptionAndPassTheOption()
    {
        var com = RenderComponent<BitStepperOptionsTest>(parameters => parameters.Add(p => p.CurrentIndex, 1));

        com.FindAll(".bit-str-btn")[0].Click();

        CollectionAssert.AreEqual(new[] { "First" }, com.Instance.OwnClicks);
        CollectionAssert.AreEqual(new[] { "First" }, com.Instance.Clicked);
        Assert.AreEqual(0, com.Instance.CurrentIndex);
    }

    [TestMethod]
    public void BitStepperShouldFollowTheParametersOfItsOptions()
    {
        var com = RenderComponent<BitStepperOptionsTest>();

        com.Render(parameters =>
        {
            parameters.Add(p => p.LastText, "Done");
            parameters.Add(p => p.LastIsError, true);
            parameters.Add(p => p.LastAriaLabel, "Done, failed");
        });

        CollectionAssert.AreEqual(new[] { "First", "Done" }, Texts(com));

        var last = com.FindAll("li.bit-str-stp")[1];
        Assert.IsTrue(last.ClassList.Contains("bit-str-err"));
        Assert.AreEqual("Done, failed", last.QuerySelector(".bit-str-btn")!.GetAttribute("aria-label"));

        com.Render(parameters => parameters.Add(p => p.LastIsDisabled, true));

        Assert.IsTrue(com.FindAll("li.bit-str-stp")[1].ClassList.Contains("bit-str-sdi"));
    }

    [TestMethod]
    public void BitStepperShouldKeepTheOrderOfTheMarkupWhenAnOptionIsAddedConditionally()
    {
        // The stepper reads the order of the option markers in the document through JS, which the test answers with
        // the markers of the rendered markup. The answer is given only once the conditional option is rendered, so the
        // calls still pending complete with the whole set, in order.
        var handler = Context.JSInterop.Setup<string[]>(ChildrenAttributes, _ => true);

        var com = RenderComponent<BitStepperOptionsTest>(parameters => parameters.Add(p => p.CurrentIndex, 1));

        CollectionAssert.AreEqual(new[] { "First", "Last" }, Texts(com));

        com.Render(parameters => parameters.Add(p => p.ShowMiddle, true));

        handler.SetResult(MarkerIds(com));

        // The middle option registered last, but it is the second step: numbered 2, and the current one.
        com.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "First", "Middle", "Last" }, Texts(com)));
        Assert.IsTrue(com.FindAll("li.bit-str-stp")[1].ClassList.Contains("bit-str-cur"));
        CollectionAssert.AreEqual(new[] { "2", "3" }, com.FindAll(".bit-str-num").Select(n => n.TextContent).ToArray());

        com.Render(parameters => parameters.Add(p => p.ShowMiddle, false));

        handler.SetResult(MarkerIds(com));

        com.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "First", "Last" }, Texts(com)));
    }

    [TestMethod]
    public void BitStepperWithOptionsShouldReadTheOrderOfTheMarkersFromItsOwnHolder()
    {
        var com = RenderComponent<BitStepperOptionsTest>();

        var read = Context.JSInterop.Invocations[ChildrenAttributes];

        Assert.IsTrue(read.Count > 0);
        Assert.AreEqual(com.Find("ol.bit-str > li:first-child").Id, read[0].Arguments[0]);
        Assert.AreEqual("data-bit-str-opt", read[0].Arguments[1]);
    }

    [TestMethod]
    public void CompactBitStepperShouldShowTheCurrentOptionAndKeepTheMarkersInAHiddenHolder()
    {
        var com = RenderComponent<BitStepperOptionsTest>(parameters =>
        {
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Compact, true);
        });

        Assert.AreEqual("Step 2 of 2", com.Find(".bit-str-pos").TextContent);
        Assert.AreEqual("Last", com.Find(".bit-str-ctx").TextContent);
        Assert.AreEqual(2, com.FindAll(".bit-str-seg").Count);
        Assert.AreEqual(0, com.FindAll("ol").Count);

        // With no list to hold them, the markers stand in a hidden element of the group, ahead of the compact part; the
        // order of the steps is read from there.
        var holder = com.Find("div.bit-str > div:first-child");

        Assert.IsTrue(holder.HasAttribute("hidden"));
        Assert.AreEqual("true", holder.GetAttribute("aria-hidden"));
        Assert.AreEqual(2, holder.QuerySelectorAll("[data-bit-str-opt]").Length);
        Assert.AreEqual(holder.Id, Context.JSInterop.Invocations[ChildrenAttributes][^1].Arguments[0]);
    }

    [TestMethod]
    public void ResponsiveCompactBitStepperShouldDrawTheStepsOfItsOptionsOnce()
    {
        var com = RenderComponent<BitStepperOptionsTest>(parameters =>
        {
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Compact, true);
            parameters.Add(p => p.Responsive, true);
        });

        CollectionAssert.AreEqual(new[] { "First", "Last" }, Texts(com));
        Assert.AreEqual(2, com.FindAll("ol.bit-str-lst > li.bit-str-stp").Count);
        Assert.AreEqual(2, com.FindAll("[data-bit-str-opt]").Count);
        Assert.AreEqual("Last", com.Find(".bit-str-ctx").TextContent);
    }

    [TestMethod]
    public void CompactBitStepperShouldReadTheTextOfACustomStep()
    {
        var com = RenderComponent<BitStepper<Phase>>(parameters =>
        {
            parameters.Add(p => p.Items, Phases());
            parameters.Add(p => p.NameSelectors, ByName);
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Compact, true);
        });

        Assert.AreEqual("Step 2 of 3", com.Find(".bit-str-pos").TextContent);
        Assert.AreEqual("Build", com.Find(".bit-str-ctx").TextContent);

        // The current step failed, which the compact part says too.
        Assert.IsTrue(com.Find(".bit-str-ctx").ClassList.Contains("bit-str-cte"));
        Assert.IsTrue(com.FindAll(".bit-str-seg")[1].ClassList.Contains("bit-str-sge"));
    }

    [TestMethod]
    public void BitStepperWithOptionsShouldIgnoreItems()
    {
        // Options are the steps once there are any: Items given beside them, or changed later, are not read at all.
        Microsoft.AspNetCore.Components.RenderFragment options = builder =>
        {
            builder.OpenComponent<BitStepperOption>(0);
            builder.AddAttribute(1, nameof(BitStepperOption.Text), "From the markup");
            builder.CloseComponent();
        };

        var com = RenderComponent<BitStepper<BitStepperOption>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitStepperOption> { new() { Text = "From Items" } });
            parameters.Add(p => p.Options, options);
        });

        CollectionAssert.AreEqual(new[] { "From the markup" }, Texts(com));

        com.Render(parameters => parameters.Add(p => p.Items, new List<BitStepperOption> { new() { Text = "New Items" } }));

        CollectionAssert.AreEqual(new[] { "From the markup" }, Texts(com));
    }

    [TestMethod]
    public void BitStepperWithItemsShouldReadNoOrderAndRenderNoHolder()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
            parameters.Add(p => p.Items, new List<BitStepperItem> { new() { Text = "One" }, new() { Text = "Two" } }));

        Assert.AreEqual(2, com.FindAll("li").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[ChildrenAttributes].Count);
    }

    private static string[] MarkerIds<T>(IRenderedComponent<T> com) where T : Microsoft.AspNetCore.Components.IComponent =>
        com.FindAll("[data-bit-str-opt]").Select(e => e.GetAttribute("data-bit-str-opt")!).ToArray();
}
