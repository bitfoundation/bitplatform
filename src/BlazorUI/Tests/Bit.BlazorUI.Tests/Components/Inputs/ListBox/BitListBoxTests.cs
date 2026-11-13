using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ListBox;

[TestClass]
public class BitListBoxTests : BunitTestContext
{
    private const string FocusElement = "Blazor._internal.domWrapper.focus";

    private static List<BitListBoxItem<string>> Fruits() =>
    [
        new() { Text = "Apple", Value = "apple", Key = "apple" },
        new() { Text = "Avocado", Value = "avocado" },
        new() { Text = "Banana", Value = "banana", IsDisabled = true },
        new() { Text = "Blueberry", Value = "blueberry" },
        new() { Text = "Cherry", Value = "cherry" }
    ];

    private static string[] Selected(IRenderedComponent<BitListBox<BitListBoxItem<string>, string>> com) =>
        com.FindAll("li[role=option][aria-selected=true]").Select(o => o.TextContent.Trim()).ToArray();

    private static string?[] TabIndexes(IRenderedComponent<BitListBox<BitListBoxItem<string>, string>> com) =>
        com.FindAll("li[role=option]").Select(o => o.GetAttribute("tabindex")).ToArray();

    private static void Key(IRenderedComponent<BitListBox<BitListBoxItem<string>, string>> com, string key, bool shift = false) =>
        com.Find("ul[role=listbox]").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

    [TestMethod]
    public void BitListBoxShouldRenderAListboxOfOptionsWithOneTabStop()
    {
        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.Label, "Fruit");
        });

        var list = com.Find("ul");
        var label = com.Find(".bit-lbx-lbl");

        Assert.AreEqual("listbox", list.GetAttribute("role"));
        Assert.AreEqual(label.Id, list.GetAttribute("aria-labelledby"));
        Assert.IsNull(list.GetAttribute("aria-multiselectable"));
        Assert.AreEqual(5, com.FindAll("li[role=option]").Count);
        Assert.IsTrue(com.FindAll("li[role=option]").All(o => o.GetAttribute("aria-selected") == "false"));

        // The list is reached by one Tab: the first item that can be selected, with nothing selected yet.
        CollectionAssert.AreEqual(new[] { "0", "-1", "-1", "-1", "-1" }, TabIndexes(com));
        Assert.AreEqual("true", com.FindAll("li")[2].GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitListBoxShouldBeNamedByItsAriaLabelWithoutALabel()
    {
        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.AriaLabel, "Fruits");
        });

        Assert.AreEqual("Fruits", com.Find("ul").GetAttribute("aria-label"));
        Assert.IsNull(com.Find("ul").GetAttribute("aria-labelledby"));
        Assert.AreEqual(0, com.FindAll(".bit-lbx-lbl").Count);
    }

    [TestMethod]
    public void BitListBoxShouldSelectAClickedItem()
    {
        string? bound = null;
        var selected = new List<string?>();
        var changes = new List<string?>();

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Bind(p => p.Value, bound, v => bound = v);
            parameters.Add(p => p.OnSelectItem, (BitListBoxItem<string> i) => selected.Add(i.Text));
            parameters.Add(p => p.OnChange, (string? v) => changes.Add(v));
        });

        com.FindAll("li")[3].Click();

        Assert.AreEqual("blueberry", bound);
        CollectionAssert.AreEqual(new[] { "Blueberry" }, Selected(com));
        CollectionAssert.AreEqual(new[] { "Blueberry" }, selected);
        CollectionAssert.AreEqual(new[] { "blueberry" }, changes);

        // The selected item is the tab stop now.
        CollectionAssert.AreEqual(new[] { "-1", "-1", "-1", "0", "-1" }, TabIndexes(com));

        // Clicking it again changes nothing, and a disabled item cannot be clicked into the selection.
        com.FindAll("li")[3].Click();
        com.FindAll("li")[2].Click();

        Assert.AreEqual("blueberry", bound);
        Assert.AreEqual(1, selected.Count);
    }

    [TestMethod]
    public void BitListBoxShouldMoveAndSelectWithTheArrowKeysPassingDisabledItemsBy()
    {
        string? bound = "avocado";

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        // The selection follows the keyboard in single select mode, over the disabled Banana.
        Key(com, "ArrowDown");

        Assert.AreEqual("blueberry", bound);
        CollectionAssert.AreEqual(new[] { "-1", "-1", "-1", "0", "-1" }, TabIndexes(com));
        Assert.IsTrue(Context.JSInterop.Invocations[FocusElement].Count > 0, "the focus is moved to the item.");

        Key(com, "End");
        Assert.AreEqual("cherry", bound);

        // The end of the list is where it stops: no wrapping.
        Key(com, "ArrowDown");
        Assert.AreEqual("cherry", bound);

        Key(com, "Home");
        Assert.AreEqual("apple", bound);

        Key(com, "ArrowUp");
        Assert.AreEqual("apple", bound);
    }

    [TestMethod]
    [DataRow("c", "cherry")]
    [DataRow("B", "blueberry")] // case aside, and past the disabled Banana
    public void BitListBoxShouldMoveToAnItemByTypingTheStartOfItsText(string key, string expected)
    {
        string? bound = null;

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        Key(com, key);

        Assert.AreEqual(expected, bound);
    }

    [TestMethod]
    public void BitListBoxShouldMoveOnToTheNextItemWhenTheSameLetterIsTypedAgain()
    {
        string? bound = null;

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        // The keyboard starts on Apple, so "a" moves on to the next one, then around to the start and on again.
        Key(com, "a");
        Assert.AreEqual("avocado", bound);
        Key(com, "a");
        Assert.AreEqual("apple", bound);
        Key(com, "a");
        Assert.AreEqual("avocado", bound);
    }

    [TestMethod]
    public void BitListBoxShouldMatchWhatIsTypedTogether()
    {
        string? bound = null;

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        Key(com, "a");
        Key(com, "v");

        Assert.AreEqual("avocado", bound);
    }

    [TestMethod]
    public void MultiSelectBitListBoxShouldAddAndTakeOutItemsOfTheSelection()
    {
        IEnumerable<string?>? bound = null;
        var selected = new List<string?>();
        var deselected = new List<string?>();

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Bind(p => p.Values, bound, v => bound = v);
            parameters.Add(p => p.OnSelectItem, (BitListBoxItem<string> i) => selected.Add(i.Text));
            parameters.Add(p => p.OnDeselectItem, (BitListBoxItem<string> i) => deselected.Add(i.Text));
        });

        Assert.AreEqual("true", com.Find("ul").GetAttribute("aria-multiselectable"));
        Assert.AreEqual(5, com.FindAll(".bit-lbx-chk").Count);

        com.FindAll("li")[0].Click();
        com.FindAll("li")[4].Click();

        CollectionAssert.AreEqual(new[] { "apple", "cherry" }, bound!.ToArray());
        CollectionAssert.AreEqual(new[] { "Apple", "Cherry" }, Selected(com));
        Assert.AreEqual(2, com.FindAll(".bit-lbx-sel .bit-lbx-chk .bit-icon--CheckMark").Count);

        com.FindAll("li")[0].Click();

        CollectionAssert.AreEqual(new[] { "cherry" }, bound!.ToArray());
        CollectionAssert.AreEqual(new[] { "Apple", "Cherry" }, selected);
        CollectionAssert.AreEqual(new[] { "Apple" }, deselected);
    }

    [TestMethod]
    public void MultiSelectBitListBoxShouldMoveWithTheArrowsAndChooseWithSpace()
    {
        IEnumerable<string?>? bound = null;

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Bind(p => p.Values, bound, v => bound = v);
        });

        // Moving only moves.
        Key(com, "ArrowDown");
        Assert.IsNull(bound);
        CollectionAssert.AreEqual(new[] { "-1", "0", "-1", "-1", "-1" }, TabIndexes(com));

        Key(com, " ");
        Key(com, "ArrowDown");
        Key(com, "Enter");

        CollectionAssert.AreEqual(new[] { "avocado", "blueberry" }, bound!.ToArray());

        // A modified Space is the browser's, not the list box's.
        Key(com, " ", shift: true);
        CollectionAssert.AreEqual(new[] { "avocado", "blueberry" }, bound!.ToArray());

        Key(com, " ");
        CollectionAssert.AreEqual(new[] { "avocado" }, bound!.ToArray());
    }

    [TestMethod]
    public void BitListBoxShouldStartWithItsDefaultValues()
    {
        var single = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.DefaultValue, "cherry");
        });

        CollectionAssert.AreEqual(new[] { "Cherry" }, Selected(single));
        CollectionAssert.AreEqual(new[] { "-1", "-1", "-1", "-1", "0" }, TabIndexes(single));

        // Uncontrolled, the user's choice stands.
        single.FindAll("li")[0].Click();
        CollectionAssert.AreEqual(new[] { "Apple" }, Selected(single));

        var multi = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DefaultValues, new[] { "apple", "blueberry" });
        });

        CollectionAssert.AreEqual(new[] { "Apple", "Blueberry" }, Selected(multi));

        multi.FindAll("li")[0].Click();
        CollectionAssert.AreEqual(new[] { "Blueberry" }, Selected(multi));
    }

    [TestMethod]
    public void BitListBoxShouldNotTakeTheDefaultOfTheValueTypeForASelection()
    {
        var items = new List<BitListBoxItem<int>>
        {
            new() { Text = "Zero", Value = 0 },
            new() { Text = "One", Value = 1 }
        };

        var com = RenderComponent<BitListBox<BitListBoxItem<int>, int>>(parameters => parameters.Add(p => p.Items, items));

        // Value is 0 while nothing is chosen, which is not a choice of Zero.
        Assert.AreEqual(0, com.FindAll("[aria-selected=true]").Count);

        com.FindAll("li")[0].Click();

        Assert.AreEqual("true", com.FindAll("li")[0].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitListBoxBoundOneWayShouldKeepItsSelection()
    {
        var single = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.Value, "apple");
        });

        single.FindAll("li")[4].Click();
        CollectionAssert.AreEqual(new[] { "Apple" }, Selected(single));

        var multi = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.Values, new[] { "apple" });
        });

        multi.FindAll("li")[4].Click();
        CollectionAssert.AreEqual(new[] { "Apple" }, Selected(multi));
    }

    [TestMethod]
    public void ReadOnlyBitListBoxShouldLetTheKeyboardMoveButNotChoose()
    {
        string? bound = "apple";

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.ReadOnly, true);
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        Assert.AreEqual("true", com.Find("ul").GetAttribute("aria-readonly"));

        Key(com, "ArrowDown");
        com.FindAll("li")[4].Click();

        Assert.AreEqual("apple", bound);
        CollectionAssert.AreEqual(new[] { "-1", "-1", "-1", "-1", "0" }, TabIndexes(com));
    }

    [TestMethod]
    public void DisabledBitListBoxShouldTakeNoFocusAndNoChoice()
    {
        string? bound = null;

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.Disabled, true);
            parameters.Bind(p => p.Value, bound, v => bound = v);
        });

        Assert.IsTrue(com.Find(".bit-lbx").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", com.Find("ul").GetAttribute("aria-disabled"));
        Assert.IsTrue(TabIndexes(com).All(t => t == "-1"));
        Assert.IsTrue(com.FindAll("li").All(o => o.GetAttribute("aria-disabled") == "true"));

        com.FindAll("li")[0].Click();
        Key(com, "ArrowDown");

        Assert.IsNull(bound);
    }

    [TestMethod]
    public void BitListBoxShouldSayWhatIsWrongWithTheSelection()
    {
        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.ErrorMessage, "Pick one");
        });

        var list = com.Find("ul");
        var error = com.Find(".bit-lbx-err");

        Assert.AreEqual("true", list.GetAttribute("aria-required"));
        Assert.AreEqual("true", list.GetAttribute("aria-invalid"));
        Assert.AreEqual(error.Id, list.GetAttribute("aria-describedby"));
        Assert.AreEqual("Pick one", error.TextContent);
        Assert.IsTrue(com.Find(".bit-lbx").ClassList.Contains("bit-inv"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.ErrorMessage, null);
            parameters.Add(p => p.Invalid, true);
        });

        Assert.AreEqual("true", com.Find("ul").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, com.FindAll(".bit-lbx-err").Count);
        Assert.IsNull(com.Find("ul").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitListBoxShouldBeValidatedWithItsEditForm()
    {
        var com = RenderComponent<BitListBoxFormTest>();

        com.Find("form").Submit();

        Assert.AreEqual(0, com.Instance.ValidSubmits);
        Assert.AreEqual("Pick a fruit", com.Find("#single .bit-lbx-err").TextContent);
        Assert.AreEqual("Pick two fruits", com.Find("#multi .bit-lbx-err").TextContent);
        Assert.AreEqual("true", com.Find("#single ul").GetAttribute("aria-invalid"));

        // A choice validates its field again: the single one at once, the multi one once it holds two.
        com.FindAll("#single li")[1].Click();
        Assert.AreEqual("banana", com.Instance.Model.Fruit);
        Assert.AreEqual(0, com.FindAll("#single .bit-lbx-err").Count);

        com.FindAll("#multi li")[0].Click();
        Assert.AreEqual("Pick two fruits", com.Find("#multi .bit-lbx-err").TextContent);
        com.FindAll("#multi li")[2].Click();
        Assert.AreEqual(0, com.FindAll("#multi .bit-lbx-err").Count);
        CollectionAssert.AreEqual(new[] { "apple", "cherry" }, com.Instance.Model.Fruits!.ToArray());

        com.Find("form").Submit();
        Assert.AreEqual(1, com.Instance.ValidSubmits);
    }

    [TestMethod]
    public void BitListBoxShouldRenderTheIconDescriptionTitleAndAriaLabelOfAnItem()
    {
        var items = Fruits();
        items[0].IconName = "Heart";
        items[0].Description = "Red or green";
        items[0].Title = "An apple a day";
        items[1].AriaLabel = "Avocado, ripe";

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters => parameters.Add(p => p.Items, items));

        var first = com.FindAll("li")[0];

        Assert.IsTrue(first.QuerySelector(".bit-lbx-ico")!.ClassList.Contains("bit-icon--Heart"));
        Assert.AreEqual("true", first.QuerySelector(".bit-lbx-ico")!.GetAttribute("aria-hidden"));
        Assert.AreEqual("Red or green", first.QuerySelector(".bit-lbx-dsc")!.TextContent);
        Assert.AreEqual("An apple a day", first.GetAttribute("title"));
        Assert.AreEqual("Avocado, ripe", com.FindAll("li")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitListBoxShouldApplyTheClassesAndStylesOfEveryPart()
    {
        var items = Fruits();
        items[0].Class = "own-class";
        items[0].Style = "margin:1px";
        items[0].IconName = "Heart";
        items[0].Description = "Red";

        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, items);
            parameters.Add(p => p.MultiSelect, true);
            parameters.Add(p => p.DefaultValues, new[] { "apple" });
            parameters.Add(p => p.Label, "Fruit");
            parameters.Add(p => p.ErrorMessage, "Wrong");
            parameters.Add(p => p.MaxHeight, "10rem");
            parameters.Add(p => p.Classes, new()
            {
                Root = "c-root", Label = "c-lbl", List = "c-lst", Item = "c-itm", SelectedItem = "c-sel", Check = "c-chk",
                Icon = "c-ico", Text = "c-txt", Description = "c-dsc", ErrorMessage = "c-err"
            });
            parameters.Add(p => p.Styles, new()
            {
                Root = "color:red", Label = "padding:1px", List = "padding:2px", Item = "padding:3px", SelectedItem = "padding:4px",
                Check = "padding:5px", Icon = "padding:6px", Text = "padding:7px", Description = "padding:8px", ErrorMessage = "padding:9px"
            });
        });

        void AssertPart(string selector, string cssClass, string? style)
        {
            var part = com.Find(selector);
            Assert.IsTrue(part.ClassList.Contains(cssClass), $"{selector} has no {cssClass}.");
            if (style is not null) StringAssert.Contains(part.GetAttribute("style"), style, selector);
        }

        AssertPart(".bit-lbx", "c-root", "color:red");
        AssertPart(".bit-lbx-lbl", "c-lbl", "padding:1px");
        AssertPart(".bit-lbx-lst", "c-lst", "max-height:10rem;padding:2px");
        AssertPart(".bit-lbx-chk", "c-chk", "padding:5px");
        AssertPart(".bit-lbx-ico", "c-ico", "padding:6px");
        AssertPart(".bit-lbx-txt", "c-txt", "padding:7px");
        AssertPart(".bit-lbx-dsc", "c-dsc", "padding:8px");
        AssertPart(".bit-lbx-err", "c-err", "padding:9px");

        var first = com.FindAll("li")[0];
        var second = com.FindAll("li")[1];

        Assert.IsTrue(first.ClassList.Contains("c-itm") && first.ClassList.Contains("c-sel") && first.ClassList.Contains("own-class"));
        Assert.AreEqual("padding:3px;padding:4px;margin:1px", first.GetAttribute("style"));
        Assert.IsTrue(second.ClassList.Contains("c-itm") && second.ClassList.Contains("c-sel") is false);
        Assert.AreEqual("padding:3px", second.GetAttribute("style"));
    }

    [TestMethod]
    public void BitListBoxShouldFocusItsTabStop()
    {
        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters =>
        {
            parameters.Add(p => p.Items, Fruits());
            parameters.Add(p => p.DefaultValue, "blueberry");
        });

        var before = Context.JSInterop.Invocations[FocusElement].Count;

        com.InvokeAsync(() => com.Instance.FocusAsync());

        Assert.AreEqual(before + 1, Context.JSInterop.Invocations[FocusElement].Count);
    }

    [TestMethod]
    public void BitListBoxShouldFollowItsItems()
    {
        var com = RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters => parameters.Add(p => p.Items, Fruits()));

        com.Render(parameters => parameters.Add(p => p.Items, Fruits().Take(2).ToList()));

        CollectionAssert.AreEqual(new[] { "Apple", "Avocado" }, com.FindAll(".bit-lbx-txt").Select(t => t.TextContent).ToArray());
    }

    [TestMethod]
    public void BitListBoxShouldKeepThePageFromScrollingOnItsKeys()
    {
        RenderComponent<BitListBox<BitListBoxItem<string>, string>>(parameters => parameters.Add(p => p.Items, Fruits()));

        var registration = Context.JSInterop.Invocations["BitBlazorUI.Utils.registerPreventKeys"].Single();

        CollectionAssert.AreEquivalent(new[] { "ArrowUp", "ArrowDown", "Home", "End", " " }, (string[])registration.Arguments[1]!);
    }
}
