using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Stepper;

[TestClass]
public class BitStepperTests : BunitTestContext
{
    private static List<BitStepperItem> Items() =>
    [
        new() { Text = "Account", Description = "Your details", Key = "account" },
        new() { Text = "Address", Key = "address" },
        new() { Text = "Payment", Key = "payment" },
        new() { Text = "Review", Key = "review" }
    ];

    private IRenderedComponent<BitStepper<BitStepperItem>> Render(
        List<BitStepperItem>? items = null,
        int currentIndex = 0,
        bool linear = false,
        bool readOnly = false,
        bool disabled = false,
        bool vertical = false)
    {
        return RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items ?? Items());
            parameters.Add(p => p.CurrentIndex, currentIndex);
            parameters.Add(p => p.Linear, linear);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.Disabled, disabled);
            parameters.Add(p => p.Vertical, vertical);
        });
    }

    private static string[] States(IRenderedComponent<BitStepper<BitStepperItem>> com) =>
        com.FindAll("li.bit-stp-stp").Select(li => li.ClassList.First(c => c is "bit-stp-cmp" or "bit-stp-cur" or "bit-stp-upc" or "bit-stp-err")).ToArray();

    [TestMethod]
    public void BitStepperShouldRenderTheStepsAsAnOrderedList()
    {
        var com = Render();

        var root = com.Find("ol.bit-stp");

        Assert.AreEqual("list", root.GetAttribute("role"));
        Assert.IsTrue(root.ClassList.Contains("bit-stp-hrz"));
        Assert.AreEqual(4, com.FindAll("li.bit-stp-stp").Count);
        CollectionAssert.AreEqual(new[] { "Account", "Address", "Payment", "Review" }, com.FindAll(".bit-stp-txt").Select(t => t.TextContent).ToArray());
        Assert.AreEqual("Your details", com.Find(".bit-stp-dsc").TextContent);

        // A line between every two steps, none after the last.
        Assert.AreEqual(3, com.FindAll(".bit-stp-con").Count);
        Assert.AreEqual(0, com.FindAll("li.bit-stp-stp")[3].QuerySelectorAll(".bit-stp-con").Length);
        Assert.IsTrue(com.FindAll(".bit-stp-con").All(c => c.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    public void BitStepperShouldLayTheStepsOutVertically()
    {
        var com = Render(vertical: true);

        var root = com.Find(".bit-stp");

        Assert.IsTrue(root.ClassList.Contains("bit-stp-vrt"));
        Assert.IsFalse(root.ClassList.Contains("bit-stp-hrz"));
    }

    [TestMethod]
    public void BitStepperShouldShowTheStateOfEveryStepFromTheCurrentIndex()
    {
        var com = Render(currentIndex: 2);

        CollectionAssert.AreEqual(new[] { "bit-stp-cmp", "bit-stp-cmp", "bit-stp-cur", "bit-stp-upc" }, States(com));

        var buttons = com.FindAll(".bit-stp-btn");

        // The current step says so; the completed ones say so in words, after their text.
        Assert.AreEqual("step", buttons[2].GetAttribute("aria-current"));
        Assert.AreEqual(1, com.FindAll("[aria-current]").Count);
        Assert.AreEqual("Completed", buttons[0].QuerySelector(".bit-stp-sts")!.TextContent);
        Assert.IsNull(buttons[2].QuerySelector(".bit-stp-sts"));
        Assert.IsNull(buttons[3].QuerySelector(".bit-stp-sts"));

        // A completed step shows the check mark, the others their number.
        Assert.IsTrue(buttons[0].QuerySelector(".bit-stp-ico")!.ClassList.Contains("bit-icon--CheckMark"));
        Assert.AreEqual("3", buttons[2].QuerySelector(".bit-stp-num")!.TextContent);
        Assert.AreEqual("4", buttons[3].QuerySelector(".bit-stp-num")!.TextContent);

        // The indicator is drawn, not read.
        Assert.IsTrue(com.FindAll(".bit-stp-ind").All(i => i.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    [DataRow(4, new[] { "bit-stp-cmp", "bit-stp-cmp", "bit-stp-cmp", "bit-stp-cmp" })]
    [DataRow(-1, new[] { "bit-stp-upc", "bit-stp-upc", "bit-stp-upc", "bit-stp-upc" })]
    public void BitStepperShouldShowEveryStepDoneOrNoneStartedOutsideTheSteps(int currentIndex, string[] expected)
    {
        var com = Render(currentIndex: currentIndex);

        CollectionAssert.AreEqual(expected, States(com));
        Assert.AreEqual(0, com.FindAll("[aria-current]").Count);
    }

    [TestMethod]
    public void BitStepperShouldShowAStepInError()
    {
        var items = Items();
        items[0].IsError = true;
        items[1].IsError = true;

        var com = Render(items, currentIndex: 1);

        CollectionAssert.AreEqual(new[] { "bit-stp-err", "bit-stp-err", "bit-stp-upc", "bit-stp-upc" }, States(com));

        var buttons = com.FindAll(".bit-stp-btn");

        Assert.IsTrue(buttons[0].QuerySelector(".bit-stp-ico")!.ClassList.Contains("bit-icon--ErrorBadge"));
        Assert.AreEqual("Error", buttons[0].QuerySelector(".bit-stp-sts")!.TextContent);

        // A step in error is still the current one when it is where the process is.
        Assert.AreEqual("step", buttons[1].GetAttribute("aria-current"));
        Assert.IsNull(buttons[0].GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitStepperShouldShowTheIconOfAStepInPlaceOfItsNumber()
    {
        var items = Items();
        items[0].IconName = "Contact";
        items[2].IconName = "Money";
        items[3].Icon = BitIconInfo.Fa("solid star");

        var com = Render(items, currentIndex: 1);

        var buttons = com.FindAll(".bit-stp-btn");

        // A completed step shows the completed icon over its own.
        Assert.IsTrue(buttons[0].QuerySelector(".bit-stp-ico")!.ClassList.Contains("bit-icon--CheckMark"));
        Assert.IsTrue(buttons[2].QuerySelector(".bit-stp-ico")!.ClassList.Contains("bit-icon--Money"));
        Assert.IsTrue(buttons[3].QuerySelector(".bit-stp-ico")!.ClassList.Contains("fa-star"));
        Assert.IsNull(buttons[2].QuerySelector(".bit-stp-num"));
    }

    [TestMethod]
    public void BitStepperShouldUseTheCompletedAndErrorIconsItIsGiven()
    {
        var items = Items();
        items[1].IsError = true;

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.CurrentIndex, 2);
            parameters.Add(p => p.CompletedIconName, "Accept");
            parameters.Add(p => p.ErrorIcon, BitIconInfo.Fa("solid xmark"));
        });

        var icons = com.FindAll(".bit-stp-ico");

        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--Accept"));
        Assert.IsTrue(icons[1].ClassList.Contains("fa-xmark"));
    }

    [TestMethod]
    public void BitStepperShouldGoToAStepThatIsClicked()
    {
        var items = Items();
        var itemClicks = new List<string?>();
        var ownClicks = new List<string?>();
        var changes = new List<int>();
        var boundChanges = new List<int>();

        items[2].OnClick = item => ownClicks.Add(item.Text);

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.CurrentIndex, 0);
            parameters.Add(p => p.CurrentIndexChanged, (int index) => boundChanges.Add(index));
            parameters.Add(p => p.OnChange, (int index) => changes.Add(index));
            parameters.Add(p => p.OnItemClick, (BitStepperItem item) => itemClicks.Add(item.Text));
        });

        com.FindAll(".bit-stp-btn")[2].Click();

        Assert.AreEqual(2, com.Instance.CurrentIndex);
        CollectionAssert.AreEqual(new[] { 2 }, boundChanges);
        CollectionAssert.AreEqual(new[] { 2 }, changes);
        CollectionAssert.AreEqual(new[] { "Payment" }, itemClicks);
        CollectionAssert.AreEqual(new[] { "Payment" }, ownClicks);
        CollectionAssert.AreEqual(new[] { "bit-stp-cmp", "bit-stp-cmp", "bit-stp-cur", "bit-stp-upc" }, States(com));

        // The current step is still a click, but goes nowhere.
        com.FindAll(".bit-stp-btn")[2].Click();

        CollectionAssert.AreEqual(new[] { "Payment", "Payment" }, itemClicks);
        CollectionAssert.AreEqual(new[] { 2 }, changes);
    }

    [TestMethod]
    public void BitStepperBoundOneWayShouldStillTellThePageTheStepTheUserAskedFor()
    {
        var changes = new List<int>();

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.CurrentIndex, 0);
            parameters.Add(p => p.OnChange, (int index) => changes.Add(index));
        });

        com.FindAll(".bit-stp-btn")[3].Click();

        // The page holds the index, and decides; the stepper stays where it was put.
        CollectionAssert.AreEqual(new[] { 3 }, changes);
        Assert.AreEqual(0, com.Instance.CurrentIndex);
        Assert.AreEqual("step", com.FindAll(".bit-stp-btn")[0].GetAttribute("aria-current"));
    }

    [TestMethod]
    public void LinearBitStepperShouldOnlyLetTheUserGoBack()
    {
        var changes = new List<int>();

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Linear, true);
            parameters.Add(p => p.OnChange, (int index) => changes.Add(index));
        });

        var steps = com.FindAll("li.bit-stp-stp");

        // The steps ahead are no controls at all: there is nothing to press there yet.
        Assert.AreEqual("BUTTON", steps[0].QuerySelector(".bit-stp-btn")!.TagName);
        Assert.AreEqual("BUTTON", steps[1].QuerySelector(".bit-stp-btn")!.TagName);
        Assert.AreEqual("DIV", steps[2].QuerySelector(".bit-stp-btn")!.TagName);
        Assert.AreEqual("DIV", steps[3].QuerySelector(".bit-stp-btn")!.TagName);

        Assert.ThrowsExactly<MissingEventHandlerException>(() => steps[2].QuerySelector(".bit-stp-btn")!.Click());
        Assert.AreEqual(0, changes.Count);

        com.FindAll("button.bit-stp-btn")[0].Click();
        CollectionAssert.AreEqual(new[] { 0 }, changes);
    }

    [TestMethod]
    public void ReadOnlyBitStepperShouldOnlyShowWhereTheProcessIs()
    {
        var changes = new List<int>();

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnChange, (int index) => changes.Add(index));
        });

        Assert.IsTrue(com.Find(".bit-stp").ClassList.Contains("bit-stp-rdo"));
        Assert.AreEqual(0, com.FindAll("button").Count);

        // The current step still says so.
        Assert.AreEqual("step", com.FindAll(".bit-stp-btn")[1].GetAttribute("aria-current"));

        Assert.ThrowsExactly<MissingEventHandlerException>(() => com.FindAll(".bit-stp-btn")[3].Click());
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void BitStepperShouldKeepTheUserOffADisabledStep()
    {
        var items = Items();
        items[2].IsDisabled = true;
        var changes = new List<int>();
        var clicks = 0;
        items[2].OnClick = _ => clicks++;

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.OnChange, (int index) => changes.Add(index));
        });

        var step = com.FindAll("li.bit-stp-stp")[2];
        var button = step.QuerySelector("button.bit-stp-btn")!;

        Assert.IsTrue(step.ClassList.Contains("bit-stp-sdi"));
        Assert.IsTrue(button.HasAttribute("disabled"));
        Assert.AreEqual("true", button.GetAttribute("aria-disabled"));

        button.Click();

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void DisabledBitStepperShouldDisableEveryStep()
    {
        var changes = new List<int>();

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Linear, true);
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnChange, (int index) => changes.Add(index));
        });

        Assert.IsTrue(com.Find(".bit-stp").ClassList.Contains("bit-dis"));

        // Every step is a disabled button, the ones a linear stepper keeps out of reach included: the whole stepper is
        // what is unavailable.
        var buttons = com.FindAll("button.bit-stp-btn");
        Assert.AreEqual(4, buttons.Count);
        Assert.IsTrue(buttons.All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));

        buttons[0].Click();
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void BitStepperShouldNameAndTitleAStepItsOwnWay()
    {
        var items = Items();
        items[1].AriaLabel = "Step 2: where to ship";
        items[1].Title = "Shipping address";

        var com = Render(items);

        var button = com.FindAll(".bit-stp-btn")[1];

        Assert.AreEqual("Step 2: where to ship", button.GetAttribute("aria-label"));
        Assert.AreEqual("Shipping address", button.GetAttribute("title"));
    }

    [TestMethod]
    public void BitStepperShouldSayTheStatesInTheWordsItIsGiven()
    {
        BitStrings.Current = new TranslatedStrings();

        try
        {
            var items = Items();
            items[2].IsError = true;

            var com = Render(items, currentIndex: 3);

            var states = com.FindAll(".bit-stp-sts").Select(s => s.TextContent).ToArray();

            CollectionAssert.AreEqual(new[] { "انجام شده", "انجام شده", "خطا" }, states);
        }
        finally
        {
            BitStrings.Current = null;
        }
    }

    private sealed class TranslatedStrings : BitStrings
    {
        public override string StepCompleted => "انجام شده";

        public override string StepError => "خطا";
    }

    [TestMethod]
    public void BitStepperShouldApplyTheClassesAndStylesOfEveryPart()
    {
        var items = Items();
        items[3].IsError = true;
        items[0].Class = "own-class";
        items[0].Style = "margin:1px";

        var classes = new BitStepperClassStyles
        {
            Root = "c-root", Step = "c-step", CompletedStep = "c-cmp", CurrentStep = "c-cur", UpcomingStep = "c-upc",
            ErrorStep = "c-err", Button = "c-btn", Indicator = "c-ind", Icon = "c-ico", Number = "c-num",
            Label = "c-lbl", Text = "c-txt", Description = "c-dsc", Connector = "c-con"
        };

        var styles = new BitStepperClassStyles
        {
            Root = "color:red", Step = "padding:1px", CompletedStep = "padding:2px", CurrentStep = "padding:3px",
            UpcomingStep = "padding:4px", ErrorStep = "padding:5px", Button = "padding:6px", Indicator = "padding:7px",
            Icon = "padding:8px", Number = "padding:9px", Label = "padding:10px", Text = "padding:11px",
            Description = "padding:12px", Connector = "padding:13px"
        };

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Classes, classes);
            parameters.Add(p => p.Styles, styles);
        });

        var root = com.Find(".bit-stp");
        var steps = com.FindAll("li.bit-stp-stp");

        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");

        Assert.IsTrue(steps.All(s => s.ClassList.Contains("c-step")));
        Assert.IsTrue(steps[0].ClassList.Contains("c-cmp") && steps[0].ClassList.Contains("own-class"));
        Assert.IsTrue(steps[1].ClassList.Contains("c-cur"));
        Assert.IsTrue(steps[2].ClassList.Contains("c-upc"));
        Assert.IsTrue(steps[3].ClassList.Contains("c-err"));

        Assert.AreEqual("padding:1px;padding:2px;margin:1px", steps[0].GetAttribute("style"));
        Assert.AreEqual("padding:1px;padding:3px", steps[1].GetAttribute("style"));
        Assert.AreEqual("padding:1px;padding:4px", steps[2].GetAttribute("style"));
        Assert.AreEqual("padding:1px;padding:5px", steps[3].GetAttribute("style"));

        void AssertPart(string selector, string cssClass, string style)
        {
            var part = com.Find(selector);
            Assert.IsTrue(part.ClassList.Contains(cssClass), $"{selector} has no {cssClass}.");
            Assert.AreEqual(style, part.GetAttribute("style"), selector);
        }

        AssertPart(".bit-stp-btn", "c-btn", "padding:6px");
        AssertPart(".bit-stp-ind", "c-ind", "padding:7px");
        AssertPart(".bit-stp-ico", "c-ico", "padding:8px");
        AssertPart(".bit-stp-num", "c-num", "padding:9px");
        AssertPart(".bit-stp-lbl", "c-lbl", "padding:10px");
        AssertPart(".bit-stp-txt", "c-txt", "padding:11px");
        AssertPart(".bit-stp-dsc", "c-dsc", "padding:12px");
        AssertPart(".bit-stp-con", "c-con", "padding:13px");
    }

    [TestMethod]
    public void BitStepperShouldFollowItsItems()
    {
        var com = Render();

        var items = Items().Take(2).ToList();
        items.Add(new() { Text = "Done", Key = "done" });

        com.Render(parameters => parameters.Add(p => p.Items, items));

        CollectionAssert.AreEqual(new[] { "Account", "Address", "Done" }, com.FindAll(".bit-stp-txt").Select(t => t.TextContent).ToArray());
        Assert.AreEqual(2, com.FindAll(".bit-stp-con").Count);
    }

    [TestMethod]
    public void BitStepperShouldRenderNoStepsWithoutItems()
    {
        var com = Render(items: []);

        Assert.AreEqual(0, com.FindAll("li").Count);
        Assert.IsNotNull(com.Find("ol.bit-stp"));
    }

    [TestMethod]
    public void BitStepperShouldBeNamedByItsAriaLabel()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.AriaLabel, "Checkout progress");
        });

        Assert.AreEqual("Checkout progress", com.Find("ol.bit-stp").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitStepperShouldRenderARightToLeftDirection()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", com.Find(".bit-stp").GetAttribute("dir"));
    }
}
