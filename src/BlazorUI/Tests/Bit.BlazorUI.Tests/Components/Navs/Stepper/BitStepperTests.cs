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
        bool vertical = false,
        bool responsive = false,
        bool compact = false)
    {
        return RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, items ?? Items());
            parameters.Add(p => p.CurrentIndex, currentIndex);
            parameters.Add(p => p.Linear, linear);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.Disabled, disabled);
            parameters.Add(p => p.Vertical, vertical);
            parameters.Add(p => p.Responsive, responsive);
            parameters.Add(p => p.Compact, compact);
        });
    }

    private static string[] Segments(IRenderedComponent<BitStepper<BitStepperItem>> com) =>
        com.FindAll(".bit-str-seg").Select(s => s.ClassList.FirstOrDefault(c => c is "bit-str-sga" or "bit-str-sge") ?? "").ToArray();

    private static string[] States(IRenderedComponent<BitStepper<BitStepperItem>> com) =>
        com.FindAll("li.bit-str-stp").Select(li => li.ClassList.First(c => c is "bit-str-cmp" or "bit-str-cur" or "bit-str-upc" or "bit-str-err")).ToArray();

    [TestMethod]
    public void BitStepperShouldRenderTheStepsAsAnOrderedList()
    {
        var com = Render();

        var root = com.Find("ol.bit-str");

        Assert.AreEqual("list", root.GetAttribute("role"));
        Assert.IsTrue(root.ClassList.Contains("bit-str-hrz"));
        Assert.AreEqual(4, com.FindAll("li.bit-str-stp").Count);
        CollectionAssert.AreEqual(new[] { "Account", "Address", "Payment", "Review" }, com.FindAll(".bit-str-txt").Select(t => t.TextContent).ToArray());
        Assert.AreEqual("Your details", com.Find(".bit-str-dsc").TextContent);

        // A line between every two steps, none after the last.
        Assert.AreEqual(3, com.FindAll(".bit-str-con").Count);
        Assert.AreEqual(0, com.FindAll("li.bit-str-stp")[3].QuerySelectorAll(".bit-str-con").Length);
        Assert.IsTrue(com.FindAll(".bit-str-con").All(c => c.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    public void BitStepperShouldLayTheStepsOutVertically()
    {
        var com = Render(vertical: true);

        var root = com.Find(".bit-str");

        Assert.IsTrue(root.ClassList.Contains("bit-str-vrt"));
        Assert.IsFalse(root.ClassList.Contains("bit-str-hrz"));
    }

    [TestMethod,
        DataRow(false, false, "bit-str-hrz", false),
        DataRow(true, false, "bit-str-rsp", true),
        DataRow(true, true, "bit-str-vrt", false),
        DataRow(false, true, "bit-str-vrt", false)]
    public void BitStepperShouldMarkAHorizontalStepperResponsive(bool responsive, bool vertical, string layout, bool wrapped)
    {
        var com = Render(vertical: vertical, responsive: responsive);

        var root = com.Find("ol.bit-str");

        // A Responsive horizontal stepper is a row or a column by its width, so it carries one layout class of its own
        // instead of either; a vertical stepper is the layout a Responsive one switches to, so it has nothing to switch.
        var layouts = new[] { "bit-str-hrz", "bit-str-vrt", "bit-str-rsp" };
        CollectionAssert.AreEqual(new[] { layout }, layouts.Where(root.ClassList.Contains).ToArray());

        // The list stands in the wrapper that is the query container of its layout only when it is Responsive.
        Assert.AreEqual(wrapped, root.ParentElement?.ClassList.Contains("bit-str-rsw") is true);
        Assert.AreEqual(wrapped ? 1 : 0, com.FindAll(".bit-str-rsw").Count);
        Assert.AreEqual(wrapped ? "DIV" : "OL", com.Nodes.OfType<AngleSharp.Dom.IElement>().First().TagName);
    }

    [TestMethod]
    public void BitStepperShouldRenderTheSameStepsWhetherResponsiveOrNot()
    {
        var plain = Render(currentIndex: 1);
        var responsive = Render(currentIndex: 1, responsive: true);

        Assert.AreEqual(plain.FindAll("li.bit-str-stp").Count, responsive.FindAll("li.bit-str-stp").Count);
        Assert.AreEqual(plain.FindAll(".bit-str-con").Count, responsive.FindAll(".bit-str-con").Count);
        CollectionAssert.AreEqual(States(plain), States(responsive));
    }

    [TestMethod]
    public void CompactBitStepperShouldShowTheCurrentStepAloneInAGroup()
    {
        var com = Render(currentIndex: 1, compact: true);

        // The steps are no list: the root is a group, named as the list is, holding the compact part alone.
        var root = com.Find(".bit-str");
        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.IsTrue(root.ClassList.Contains("bit-str-cpt"));
        Assert.IsFalse(new[] { "bit-str-hrz", "bit-str-vrt", "bit-str-rsp" }.Any(root.ClassList.Contains));
        Assert.AreEqual(0, com.FindAll("ol").Count);
        Assert.AreEqual(0, com.FindAll("li").Count);
        Assert.AreEqual(0, com.FindAll(".bit-str-rsw").Count);

        // The position in words, then the text of the step; the bar draws a segment per step, hidden from a reader.
        var summary = com.Find(".bit-str-cpt > .bit-str-sum");
        Assert.AreEqual("Step 2 of 4", summary.QuerySelector(".bit-str-pos")!.TextContent);
        Assert.AreEqual("Address", summary.QuerySelector(".bit-str-ctx")!.TextContent);
        Assert.IsNull(summary.QuerySelector(".bit-str-sts"));
        Assert.AreEqual("true", summary.QuerySelector(".bit-str-prg")!.GetAttribute("aria-hidden"));
        CollectionAssert.AreEqual(new[] { "bit-str-sga", "bit-str-sga", "", "" }, Segments(com));
    }

    [TestMethod]
    public void CompactBitStepperShouldSayAStepInErrorAndPaintItsSegment()
    {
        var items = Items();
        items[0].IsError = true;
        items[1].IsError = true;

        var com = Render(items, currentIndex: 1, compact: true);

        Assert.IsTrue(com.Find(".bit-str-ctx").ClassList.Contains("bit-str-cte"));
        Assert.AreEqual("Error", com.Find(".bit-str-sum .bit-str-sts").TextContent);
        CollectionAssert.AreEqual(new[] { "bit-str-sge", "bit-str-sge", "", "" }, Segments(com));
    }

    [TestMethod]
    [DataRow(4, new[] { "bit-str-sga", "bit-str-sga", "bit-str-sga", "bit-str-sga" })]
    [DataRow(-1, new[] { "", "", "", "" })]
    public void CompactBitStepperShouldShowTheBarAloneOutsideTheSteps(int currentIndex, string[] expected)
    {
        var com = Render(currentIndex: currentIndex, compact: true);

        Assert.AreEqual(0, com.FindAll(".bit-str-pos").Count);
        Assert.AreEqual(0, com.FindAll(".bit-str-ctx").Count);
        CollectionAssert.AreEqual(expected, Segments(com));
    }

    [TestMethod]
    public void CompactBitStepperShouldRenderNothingButItsGroupWithoutSteps()
    {
        var com = Render(items: [], compact: true);

        Assert.IsNotNull(com.Find("div.bit-str > .bit-str-sum"));
        Assert.AreEqual(0, com.FindAll(".bit-str-sum *").Count);
    }

    [TestMethod,
        DataRow(true, false, true),
        DataRow(true, true, false),
        DataRow(false, true, false),
        DataRow(false, false, false)]
    public void CompactBitStepperShouldHoldTheListOnlyWhenItIsAResponsiveRow(bool responsive, bool vertical, bool withList)
    {
        var com = Render(currentIndex: 1, compact: true, responsive: responsive, vertical: vertical);

        var root = com.Find(".bit-str");

        Assert.IsTrue(root.ClassList.Contains("bit-str-cpt"));
        Assert.AreEqual(withList, root.ParentElement?.ClassList.Contains("bit-str-rsw") is true);
        Assert.AreEqual(withList ? 1 : 0, com.FindAll(".bit-str-rsw").Count);
        Assert.AreEqual(1, com.FindAll(".bit-str-sum").Count);

        // The list a Responsive one shows while it is wide enough is always a row, and the same steps as the list of a
        // stepper that is not Compact.
        var list = com.FindAll(".bit-str-cpt > ol.bit-str-lst");
        Assert.AreEqual(withList ? 1 : 0, list.Count);
        if (withList is false) return;

        Assert.AreEqual("list", list[0].GetAttribute("role"));
        Assert.IsTrue(list[0].ClassList.Contains("bit-str-hrz"));
        CollectionAssert.AreEqual(States(Render(currentIndex: 1)), States(com));
        Assert.AreEqual("step", com.Find("li.bit-str-cur .bit-str-btn").GetAttribute("aria-current"));
        Assert.AreEqual(3, com.FindAll(".bit-str-con").Count);
    }

    [TestMethod]
    public void CompactBitStepperShouldTakeItsNameIdAndDirectionOnItsGroup()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.Compact, true);
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.AriaLabel, "Checkout progress");
            parameters.Add(p => p.Id, "checkout");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = com.Find("div.bit-str");

        Assert.AreEqual("Checkout progress", root.GetAttribute("aria-label"));
        Assert.AreEqual("checkout", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsNull(com.Find("ol.bit-str-lst").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void CompactBitStepperShouldGoBackToTheListWhenItIsNoLongerCompact()
    {
        var com = Render(currentIndex: 1, compact: true);

        com.Render(parameters => parameters.Add(p => p.Compact, false));

        var root = com.Find("ol.bit-str");
        Assert.IsTrue(root.ClassList.Contains("bit-str-hrz"));
        Assert.IsFalse(root.ClassList.Contains("bit-str-cpt"));
        Assert.AreEqual(0, com.FindAll(".bit-str-sum").Count);
        Assert.AreEqual(4, com.FindAll("li.bit-str-stp").Count);
    }

    [TestMethod]
    public void CompactBitStepperShouldRenderItsTemplateWithWhereTheProcessIs()
    {
        var contexts = new List<BitStepperCompactContext<BitStepperItem>>();

        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.CurrentIndex, 2);
            parameters.Add(p => p.Compact, true);
            parameters.Add(p => p.CompactTemplate, context => builder =>
            {
                contexts.Add(context);
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "own-compact");
                builder.AddContent(2, $"{context.Index + 1}/{context.Count} {context.Item?.Text}");
                builder.CloseElement();
            });
        });

        // The template takes the place of the position, the text and the bar, inside the compact part.
        Assert.AreEqual("3/4 Payment", com.Find(".bit-str-sum > .own-compact").TextContent);
        Assert.AreEqual(0, com.FindAll(".bit-str-pos").Count);
        Assert.AreEqual(0, com.FindAll(".bit-str-prg").Count);

        var last = contexts[^1];
        Assert.AreEqual(2, last.Index);
        Assert.AreEqual(4, last.Count);
        Assert.AreEqual("Payment", last.Item?.Text);

        com.Render(parameters => parameters.Add(p => p.CurrentIndex, 4));

        Assert.AreEqual(4, contexts[^1].Index);
        Assert.IsNull(contexts[^1].Item);
        Assert.AreEqual("5/4 ", com.Find(".own-compact").TextContent);
    }

    [TestMethod]
    public void BitStepperShouldShowTheStateOfEveryStepFromTheCurrentIndex()
    {
        var com = Render(currentIndex: 2);

        CollectionAssert.AreEqual(new[] { "bit-str-cmp", "bit-str-cmp", "bit-str-cur", "bit-str-upc" }, States(com));

        var buttons = com.FindAll(".bit-str-btn");

        // The current step says so; the completed ones say so in words, after their text.
        Assert.AreEqual("step", buttons[2].GetAttribute("aria-current"));
        Assert.AreEqual(1, com.FindAll("[aria-current]").Count);
        Assert.AreEqual("Completed", buttons[0].QuerySelector(".bit-str-sts")!.TextContent);
        Assert.IsNull(buttons[2].QuerySelector(".bit-str-sts"));
        Assert.IsNull(buttons[3].QuerySelector(".bit-str-sts"));

        // A completed step shows the check mark, the others their number.
        Assert.IsTrue(buttons[0].QuerySelector(".bit-str-ico")!.ClassList.Contains("bit-icon--CheckMark"));
        Assert.AreEqual("3", buttons[2].QuerySelector(".bit-str-num")!.TextContent);
        Assert.AreEqual("4", buttons[3].QuerySelector(".bit-str-num")!.TextContent);

        // The indicator is drawn, not read.
        Assert.IsTrue(com.FindAll(".bit-str-ind").All(i => i.GetAttribute("aria-hidden") == "true"));
    }

    [TestMethod]
    [DataRow(4, new[] { "bit-str-cmp", "bit-str-cmp", "bit-str-cmp", "bit-str-cmp" })]
    [DataRow(-1, new[] { "bit-str-upc", "bit-str-upc", "bit-str-upc", "bit-str-upc" })]
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

        CollectionAssert.AreEqual(new[] { "bit-str-err", "bit-str-err", "bit-str-upc", "bit-str-upc" }, States(com));

        var buttons = com.FindAll(".bit-str-btn");

        Assert.IsTrue(buttons[0].QuerySelector(".bit-str-ico")!.ClassList.Contains("bit-icon--ErrorBadge"));
        Assert.AreEqual("Error", buttons[0].QuerySelector(".bit-str-sts")!.TextContent);

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

        var buttons = com.FindAll(".bit-str-btn");

        // A completed step shows the completed icon over its own.
        Assert.IsTrue(buttons[0].QuerySelector(".bit-str-ico")!.ClassList.Contains("bit-icon--CheckMark"));
        Assert.IsTrue(buttons[2].QuerySelector(".bit-str-ico")!.ClassList.Contains("bit-icon--Money"));
        Assert.IsTrue(buttons[3].QuerySelector(".bit-str-ico")!.ClassList.Contains("fa-star"));
        Assert.IsNull(buttons[2].QuerySelector(".bit-str-num"));
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

        var icons = com.FindAll(".bit-str-ico");

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

        com.FindAll(".bit-str-btn")[2].Click();

        Assert.AreEqual(2, com.Instance.CurrentIndex);
        CollectionAssert.AreEqual(new[] { 2 }, boundChanges);
        CollectionAssert.AreEqual(new[] { 2 }, changes);
        CollectionAssert.AreEqual(new[] { "Payment" }, itemClicks);
        CollectionAssert.AreEqual(new[] { "Payment" }, ownClicks);
        CollectionAssert.AreEqual(new[] { "bit-str-cmp", "bit-str-cmp", "bit-str-cur", "bit-str-upc" }, States(com));

        // The current step is still a click, but goes nowhere.
        com.FindAll(".bit-str-btn")[2].Click();

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

        com.FindAll(".bit-str-btn")[3].Click();

        // The page holds the index, and decides; the stepper stays where it was put.
        CollectionAssert.AreEqual(new[] { 3 }, changes);
        Assert.AreEqual(0, com.Instance.CurrentIndex);
        Assert.AreEqual("step", com.FindAll(".bit-str-btn")[0].GetAttribute("aria-current"));
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

        var steps = com.FindAll("li.bit-str-stp");

        // The steps ahead are no controls at all: there is nothing to press there yet.
        Assert.AreEqual("BUTTON", steps[0].QuerySelector(".bit-str-btn")!.TagName);
        Assert.AreEqual("BUTTON", steps[1].QuerySelector(".bit-str-btn")!.TagName);
        Assert.AreEqual("DIV", steps[2].QuerySelector(".bit-str-btn")!.TagName);
        Assert.AreEqual("DIV", steps[3].QuerySelector(".bit-str-btn")!.TagName);

        Assert.ThrowsExactly<MissingEventHandlerException>(() => steps[2].QuerySelector(".bit-str-btn")!.Click());
        Assert.AreEqual(0, changes.Count);

        com.FindAll("button.bit-str-btn")[0].Click();
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

        Assert.IsTrue(com.Find(".bit-str").ClassList.Contains("bit-str-rdo"));
        Assert.AreEqual(0, com.FindAll("button").Count);

        // The current step still says so.
        Assert.AreEqual("step", com.FindAll(".bit-str-btn")[1].GetAttribute("aria-current"));

        Assert.ThrowsExactly<MissingEventHandlerException>(() => com.FindAll(".bit-str-btn")[3].Click());
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

        var step = com.FindAll("li.bit-str-stp")[2];
        var button = step.QuerySelector("button.bit-str-btn")!;

        Assert.IsTrue(step.ClassList.Contains("bit-str-sdi"));
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

        Assert.IsTrue(com.Find(".bit-str").ClassList.Contains("bit-dis"));

        // Every step is a disabled button, the ones a linear stepper keeps out of reach included: the whole stepper is
        // what is unavailable.
        var buttons = com.FindAll("button.bit-str-btn");
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

        var button = com.FindAll(".bit-str-btn")[1];

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

            var states = com.FindAll(".bit-str-sts").Select(s => s.TextContent).ToArray();

            CollectionAssert.AreEqual(new[] { "انجام شده", "انجام شده", "خطا" }, states);
        }
        finally
        {
            BitStrings.Current = null;
        }
    }

    [TestMethod]
    public void CompactBitStepperShouldSayThePositionInTheWordsItIsGiven()
    {
        BitStrings.Current = new TranslatedStrings();

        try
        {
            var com = Render(currentIndex: 2, compact: true);

            Assert.AreEqual("مرحله 3 از 4", com.Find(".bit-str-pos").TextContent);
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

        public override string StepPositionFormat => "مرحله {0} از {1}";
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

        var root = com.Find(".bit-str");
        var steps = com.FindAll("li.bit-str-stp");

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

        AssertPart(".bit-str-btn", "c-btn", "padding:6px");
        AssertPart(".bit-str-ind", "c-ind", "padding:7px");
        AssertPart(".bit-str-ico", "c-ico", "padding:8px");
        AssertPart(".bit-str-num", "c-num", "padding:9px");
        AssertPart(".bit-str-lbl", "c-lbl", "padding:10px");
        AssertPart(".bit-str-txt", "c-txt", "padding:11px");
        AssertPart(".bit-str-dsc", "c-dsc", "padding:12px");
        AssertPart(".bit-str-con", "c-con", "padding:13px");
    }

    [TestMethod]
    public void CompactBitStepperShouldApplyTheClassesAndStylesOfItsParts()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.CurrentIndex, 1);
            parameters.Add(p => p.Compact, true);
            parameters.Add(p => p.Classes, new() { Root = "c-root", Compact = "c-cpt", Position = "c-pos", CompactText = "c-ctx", Progress = "c-prg", Segment = "c-seg" });
            parameters.Add(p => p.Styles, new() { Root = "color:red", Compact = "padding:1px", Position = "padding:2px", CompactText = "padding:3px", Progress = "padding:4px", Segment = "padding:5px" });
        });

        var root = com.Find(".bit-str");
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");

        void AssertPart(string selector, string cssClass, string style)
        {
            foreach (var part in com.FindAll(selector))
            {
                Assert.IsTrue(part.ClassList.Contains(cssClass), $"{selector} has no {cssClass}.");
                Assert.AreEqual(style, part.GetAttribute("style"), selector);
            }
        }

        AssertPart(".bit-str-sum", "c-cpt", "padding:1px");
        AssertPart(".bit-str-pos", "c-pos", "padding:2px");
        AssertPart(".bit-str-ctx", "c-ctx", "padding:3px");
        AssertPart(".bit-str-prg", "c-prg", "padding:4px");
        AssertPart(".bit-str-seg", "c-seg", "padding:5px");
        Assert.AreEqual(4, com.FindAll(".c-seg").Count);
    }

    [TestMethod]
    public void DisabledCompactBitStepperShouldSaySoOnItsGroup()
    {
        var com = Render(currentIndex: 1, compact: true, disabled: true);

        Assert.IsTrue(com.Find("div.bit-str").ClassList.Contains("bit-dis"));
        Assert.AreEqual(0, com.FindAll("button").Count);
    }

    [TestMethod]
    public void BitStepperShouldFollowItsItems()
    {
        var com = Render();

        var items = Items().Take(2).ToList();
        items.Add(new() { Text = "Done", Key = "done" });

        com.Render(parameters => parameters.Add(p => p.Items, items));

        CollectionAssert.AreEqual(new[] { "Account", "Address", "Done" }, com.FindAll(".bit-str-txt").Select(t => t.TextContent).ToArray());
        Assert.AreEqual(2, com.FindAll(".bit-str-con").Count);
    }

    [TestMethod]
    public void BitStepperShouldRenderNoStepsWithoutItems()
    {
        var com = Render(items: []);

        Assert.AreEqual(0, com.FindAll("li").Count);
        Assert.IsNotNull(com.Find("ol.bit-str"));
    }

    [TestMethod]
    public void BitStepperShouldBeNamedByItsAriaLabel()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.AriaLabel, "Checkout progress");
        });

        Assert.AreEqual("Checkout progress", com.Find("ol.bit-str").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitStepperShouldRenderARightToLeftDirection()
    {
        var com = RenderComponent<BitStepper<BitStepperItem>>(parameters =>
        {
            parameters.Add(p => p.Items, Items());
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", com.Find(".bit-str").GetAttribute("dir"));
    }
}
