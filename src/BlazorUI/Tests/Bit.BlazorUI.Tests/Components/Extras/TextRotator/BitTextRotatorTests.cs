using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextRotator;

[TestClass]
public class BitTextRotatorTests : BunitTestContext
{
    private static readonly string[] _items = ["web", "mobile", "desktop"];

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    [TestMethod]
    public void BitTextRotatorShouldRenderEveryItemInOneSpanWithTheFirstOneShownAtRest()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Items, _items);
        });

        var root = component.Find(".bit-trt");
        var items = component.FindAll(".bit-trt-itm");

        Assert.AreEqual("SPAN", root.TagName);
        CollectionAssert.AreEqual(_items, items.Select(i => i.TextContent).ToArray());

        // The item shown first is shown at rest: the entrance is only played by an item that takes another's place.
        Assert.IsTrue(items[0].ClassList.Contains("bit-trt-cur"));
        Assert.IsFalse(items[0].ClassList.Contains("bit-trt-ent"));
        Assert.IsFalse(items[1].ClassList.Contains("bit-trt-cur"));
        Assert.IsFalse(items[2].ClassList.Contains("bit-trt-cur"));
        Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);
    }

    [TestMethod]
    public void BitTextRotatorShouldHideTheItemsAndGiveAssistiveTechnologiesAllOfThemAsOneText()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Items, ["web", "", "desktop"]);
        });

        foreach (var item in component.FindAll(".bit-trt-itm"))
        {
            Assert.AreEqual("true", item.GetAttribute("aria-hidden"));
        }

        var text = component.Find(".bit-trt-srt");

        Assert.IsFalse(text.HasAttribute("aria-hidden"));
        // An empty item is nothing to read, so it adds no separator either.
        Assert.AreEqual("web, desktop", text.TextContent);
    }

    [TestMethod]
    public void BitTextRotatorShouldRenderNothingInsideWithoutItems()
    {
        var component = RenderComponent<BitTextRotator>();

        var root = component.Find(".bit-trt");

        Assert.AreEqual(0, root.ChildElementCount);
    }

    [TestMethod,
        DataRow("h2", "H2"),
        DataRow(" p ", "P"),
        DataRow(null, "SPAN"),
        DataRow("", "SPAN"),
        DataRow("<script>", "SPAN"),
        DataRow("div onclick=x", "SPAN")]
    public void BitTextRotatorShouldRespectElement(string? element, string expectedTag)
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.Items, _items);
        });

        Assert.AreEqual(expectedTag, component.Find(".bit-trt").TagName);
    }

    [TestMethod]
    public void BitTextRotatorShouldRenderNoItemIntoAVoidElement()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Element, "br");
            parameters.Add(p => p.Items, _items);
        });

        Assert.AreEqual(0, component.Find(".bit-trt").ChildElementCount);
    }

    [TestMethod]
    public void BitTextRotatorShouldTurnToTheNextItemEveryInterval()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-trt-itm");

            Assert.IsTrue(items[1].ClassList.Contains("bit-trt-cur"));
            Assert.IsTrue(items[1].ClassList.Contains("bit-trt-ent"));
            Assert.IsTrue(items[0].ClassList.Contains("bit-trt-prv"));
            Assert.IsFalse(items[0].ClassList.Contains("bit-trt-cur"));
        }, _timeout);
    }

    [TestMethod]
    public void BitTextRotatorShouldStartOverFromTheFirstItemAfterTheLastOne()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.FindAll(".bit-trt-itm")[2].ClassList.Contains("bit-trt-cur")), _timeout);

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-trt-itm");

            // Back on the first item, which now comes in as the last one goes out.
            Assert.IsTrue(items[0].ClassList.Contains("bit-trt-cur"));
            Assert.IsTrue(items[0].ClassList.Contains("bit-trt-ent"));
            Assert.IsTrue(items[2].ClassList.Contains("bit-trt-prv"));
        }, _timeout);
    }

    [TestMethod]
    public async Task BitTextRotatorShouldNotRotateWhilePaused()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
            parameters.Add(p => p.Paused, true);
        });

        // A few intervals worth of quiet proves no timer is ticking behind the paused state.
        await Task.Delay(300);

        Assert.IsTrue(component.FindAll(".bit-trt-itm")[0].ClassList.Contains("bit-trt-cur"));
        Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);

        component.Render(parameters => parameters.Add(p => p.Paused, false));

        component.WaitForAssertion(() => Assert.IsTrue(component.FindAll(".bit-trt-itm")[1].ClassList.Contains("bit-trt-cur")), _timeout);
    }

    [TestMethod]
    public async Task BitTextRotatorShouldHoldTheItemItShowsOncePaused()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-trt-prv").Count), _timeout);

        // A tick that was already on its way when the pause was rendered finds the rotator paused and does nothing, so
        // the item shown once the render is done is the one it holds.
        component.Render(parameters => parameters.Add(p => p.Paused, true));

        var shown = CurrentIndex(component);

        await Task.Delay(300);

        Assert.AreEqual(shown, CurrentIndex(component));
    }

    [TestMethod]
    public async Task BitTextRotatorShouldNotRotateWhileDisabled()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        var root = component.Find(".bit-trt");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));

        await Task.Delay(300);

        Assert.IsTrue(component.FindAll(".bit-trt-itm")[0].ClassList.Contains("bit-trt-cur"));
        Assert.IsNull(GetTimer(component.Instance));
    }

    [TestMethod]
    public void BitTextRotatorShouldNotWriteAriaDisabledWhileEnabled()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Items, _items);
        });

        var root = component.Find(".bit-trt");

        Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        Assert.IsFalse(root.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public async Task BitTextRotatorShouldNotRotateASingleItem()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, ["web"]);
        });

        await Task.Delay(300);

        var item = component.Find(".bit-trt-itm");

        Assert.IsTrue(item.ClassList.Contains("bit-trt-cur"));
        Assert.IsFalse(item.ClassList.Contains("bit-trt-ent"));
        Assert.IsNull(GetTimer(component.Instance));
    }

    [TestMethod]
    public void BitTextRotatorShouldStartOverAtRestWhenTheItemsChange()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-trt-prv").Count), _timeout);

        string[] next = ["one", "two"];

        // Paused along with the change, so no tick moves the rotator on before it is read.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Items, next);
            parameters.Add(p => p.Paused, true);
        });

        var items = component.FindAll(".bit-trt-itm");

        CollectionAssert.AreEqual(next, items.Select(i => i.TextContent).ToArray());
        Assert.IsTrue(items[0].ClassList.Contains("bit-trt-cur"));
        Assert.IsFalse(items[0].ClassList.Contains("bit-trt-ent"));
        Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);
    }

    [TestMethod]
    public async Task BitTextRotatorShouldShowTheFirstOfNewItemsForAWholeInterval()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 1000d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-trt-prv").Count), _timeout);

        // Most of the way into the next turn of the rotation the new items replace.
        await Task.Delay(700);

        component.Render(parameters => parameters.Add(p => p.Items, ["one", "two"]));

        // Past the moment that turn was due, but well short of a whole Interval from the change.
        await Task.Delay(500);

        Assert.AreEqual(0, CurrentIndex(component));
        Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);
    }

    [TestMethod]
    public async Task BitTextRotatorShouldNotRotateUnderReducedMotion()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.prefersReducedMotion", _ => true).SetResult(true);

        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
            parameters.Add(p => p.Styles, new() { CurrentItem = "color:red" });
        });

        await Task.Delay(300);

        // The stylesheet shows the first item alone, so it stays the current one and keeps its style.
        Assert.AreEqual(0, CurrentIndex(component));
        Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);
        StringAssert.Contains(component.FindAll(".bit-trt-itm")[0].GetAttribute("style"), "color:red");
    }

    // A page's ForceAnimation switch (a bit-fam put on an ancestor) changes what the preference reads as, and nothing
    // tells the rotator so but its next turn.
    [TestMethod]
    public async Task BitTextRotatorShouldFollowAChangeOfTheMotionPreference()
    {
        var reducedMotion = true;
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.prefersReducedMotion", _ => reducedMotion).SetResult(true);
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.prefersReducedMotion", _ => reducedMotion is false).SetResult(false);

        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        // Nothing renders while the motion is reduced, so a few intervals are waited for rather than a render.
        await Task.Delay(300);

        Assert.IsTrue(Context.JSInterop.Invocations["BitBlazorUI.Utils.prefersReducedMotion"].Count > 1);
        Assert.AreEqual(0, CurrentIndex(component));

        // Animation forced back on: the rotation starts.
        reducedMotion = false;

        component.WaitForAssertion(() => Assert.AreEqual(2, CurrentIndex(component)), _timeout);

        // And off again: back on the first item, at rest, where the stylesheet shows it.
        reducedMotion = true;

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, CurrentIndex(component));
            Assert.IsFalse(component.FindAll(".bit-trt-itm")[0].ClassList.Contains("bit-trt-ent"));
            Assert.AreEqual(0, component.FindAll(".bit-trt-prv").Count);
        }, _timeout);
    }

    [TestMethod]
    public async Task BitTextRotatorShouldNotTurnTwiceWhileThePreferenceIsBeingRead()
    {
        // An answer that is held back for several ticks.
        var answer = Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.prefersReducedMotion", _ => true);

        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        await Task.Delay(300);

        Assert.AreEqual(1, answer.Invocations.Count);
        Assert.AreEqual(0, CurrentIndex(component));

        answer.SetResult(false);

        component.WaitForAssertion(() => Assert.AreEqual(1, CurrentIndex(component)), _timeout);
    }

    [TestMethod,
        DataRow(null, false),
        DataRow("span", false),
        DataRow("b", false),
        DataRow("h2", true),
        DataRow("P", true),
        DataRow("div", true)]
    public void BitTextRotatorShouldLayItselfOutAsABlockForABlockTag(string? element, bool block)
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.Items, _items);
        });

        Assert.AreEqual(block, component.Find(".bit-trt").ClassList.Contains("bit-trt-blk"));
    }

    [TestMethod]
    public void BitTextRotatorShouldFollowAChangeOfElementWithItsBlockClass()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Items, _items);
        });

        Assert.IsFalse(component.Find(".bit-trt").ClassList.Contains("bit-trt-blk"));

        component.Render(parameters => parameters.Add(p => p.Element, "div"));

        Assert.IsTrue(component.Find(".bit-trt").ClassList.Contains("bit-trt-blk"));

        component.Render(parameters => parameters.Add(p => p.Element, "span"));

        Assert.IsFalse(component.Find(".bit-trt").ClassList.Contains("bit-trt-blk"));
    }

    [TestMethod]
    public void BitTextRotatorShouldKeepItsPlaceWhenTheSameItemsArePassedAgain()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-trt-prv").Count), _timeout);

        component.Render(parameters => parameters.Add(p => p.Paused, true));

        var before = CurrentIndex(component);

        // A new collection holding the same items - what a parent writing the items inline passes on every render.
        component.Render(parameters => parameters.Add(p => p.Items, _items.ToList()));

        Assert.AreEqual(before, CurrentIndex(component));
        Assert.AreEqual(1, component.FindAll(".bit-trt-prv").Count);
    }

    [TestMethod,
        DataRow(0d),
        DataRow(-100d),
        DataRow(double.NaN),
        DataRow(double.PositiveInfinity)]
    public void BitTextRotatorShouldFallBackFromAnIntervalTheTimerRefuses(double interval)
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, interval);
            parameters.Add(p => p.Items, _items);
        });

        var timer = GetTimer(component.Instance);

        Assert.IsNotNull(timer);
        Assert.IsTrue(timer.Enabled);
        Assert.AreEqual(double.IsPositiveInfinity(interval) ? int.MaxValue : 3000, timer.Interval);
    }

    [TestMethod]
    public void BitTextRotatorShouldNotStartItsTimerBeforeItIsRendered()
    {
        var rotator = new BitTextRotator();

        Assert.IsNull(GetTimer(rotator));
    }

    [TestMethod]
    public async Task BitTextRotatorShouldDisposeItsTimer()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        var rotator = component.Instance;

        Assert.IsNotNull(GetTimer(rotator));

        await Context.DisposeComponentsAsync();

        Assert.IsNull(GetTimer(rotator));
    }

    [TestMethod]
    public void BitTextRotatorShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { Root = "root-class", Item = "item-class", CurrentItem = "current-class" });
            parameters.Add(p => p.Styles, new() { Root = "color:red", Item = "padding:1px", CurrentItem = "font-weight:bold" });
            parameters.Add(p => p.Items, _items);
        });

        var root = component.Find(".bit-trt");
        var items = component.FindAll(".bit-trt-itm");

        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");

        Assert.IsTrue(items.All(i => i.ClassList.Contains("item-class")));
        Assert.IsTrue(items.All(i => i.GetAttribute("style")!.Contains("padding:1px")));

        Assert.IsTrue(items[0].ClassList.Contains("current-class"));
        StringAssert.Contains(items[0].GetAttribute("style"), "font-weight:bold");
        Assert.IsFalse(items[1].ClassList.Contains("current-class"));
        Assert.IsFalse(items[1].GetAttribute("style")!.Contains("font-weight:bold"));
    }

    [TestMethod]
    public void BitTextRotatorShouldMoveTheCurrentItemPartWithTheRotation()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Classes, new() { CurrentItem = "current-class" });
            parameters.Add(p => p.Interval, 50d);
            parameters.Add(p => p.Items, _items);
        });

        component.WaitForAssertion(() =>
        {
            var items = component.FindAll(".bit-trt-itm");
            var current = items.Single(i => i.ClassList.Contains("bit-trt-cur"));

            Assert.AreNotSame(items[0], current);
            Assert.IsTrue(current.ClassList.Contains("current-class"));
            Assert.AreEqual(1, items.Count(i => i.ClassList.Contains("current-class")));
        }, _timeout);
    }

    [TestMethod]
    public void BitTextRotatorShouldMergeTheSplattedClassAndStyle()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Class, "own-class");
            parameters.Add(p => p.Style, "margin:1px");
            parameters.Add(p => p.Items, _items);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object>
            {
                ["class"] = "splatted-class",
                ["style"] = "padding:2px",
                ["data-test"] = "x",
            });
        });

        var root = component.Find(".bit-trt");

        Assert.IsTrue(root.ClassList.Contains("own-class"));
        Assert.IsTrue(root.ClassList.Contains("splatted-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
        Assert.AreEqual("x", root.GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitTextRotatorShouldRespectDir()
    {
        var component = RenderComponent<BitTextRotator>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.Items, _items);
        });

        var root = component.Find(".bit-trt");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    private static int CurrentIndex(IRenderedComponent<BitTextRotator> component)
        => component.FindAll(".bit-trt-itm").ToList().FindIndex(i => i.ClassList.Contains("bit-trt-cur"));

    private static System.Timers.Timer? GetTimer(BitTextRotator rotator)
        => (System.Timers.Timer?)typeof(BitTextRotator).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(rotator);
}
