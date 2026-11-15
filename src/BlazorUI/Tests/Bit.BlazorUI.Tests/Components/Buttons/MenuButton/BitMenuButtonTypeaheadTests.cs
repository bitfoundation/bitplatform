using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

// The typeahead of the menus: the search string is built on the .NET side with the rules every typeahead of the
// library shares (BitTypeahead) and handed to the script, which matches it against the rendered labels in the
// culture it is given. These tests pin what is handed over; the E2E tests pin what the script does with it.
[TestClass]
public class BitMenuButtonTypeaheadTests : BunitTestContext
{
    private const string FocusItem = "BitBlazorUI.MenuButtons.focusItem";
    private const string Menu = ".bit-mnb-cal:not(.bit-mnb-sub)";

    private CultureInfo? _culture;

    [TestInitialize]
    public void UseEnglish()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [TestCleanup]
    public void RestoreCulture()
    {
        if (_culture is not null) CultureInfo.CurrentCulture = _culture;
    }

    private static List<BitMenuButtonItem> Items() =>
    [
        new() { Text = "Apple", Key = "apple" },
        new() { Text = "Banana", Key = "banana" },
        new() { Text = "Blueberry", Key = "blueberry" },
        new()
        {
            Text = "Share",
            Key = "share",
            ChildItems = [new() { Text = "Email", Key = "email" }]
        }
    ];

    private IRenderedComponent<BitMenuButton<BitMenuButtonItem>> RenderOpen()
    {
        var com = RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
        {
            parameters.Add(p => p.Text, "Fruits");
            parameters.Add(p => p.Items, Items());
        });

        com.Find(".bit-mnb-opb").Click();

        return com;
    }

    private static void Type(IRenderedComponent<BitMenuButton<BitMenuButtonItem>> com, string selector, string key)
    {
        com.Find(selector).KeyDown(new KeyboardEventArgs { Key = key });
    }

    // What each keystroke handed to the script: the search string and whether the focused item is searched too.
    private List<(string? Text, bool FromCurrent)> Searches()
    {
        return Context.JSInterop.Invocations[FocusItem]
                      .Where(i => (string?)i.Arguments[1] == "char")
                      .Select(i => ((string?)i.Arguments[2], (bool)i.Arguments[4]!))
                      .ToList();
    }

    [TestMethod]
    public void BitMenuButtonShouldCycleWhenTheSameLetterIsTypedAgainInTheOtherCase()
    {
        var com = RenderOpen();

        Type(com, Menu, "b");
        Type(com, Menu, "B");

        // The same letter in the other case is still a repeat: the next item starting with it, not a search for "bB".
        CollectionAssert.AreEqual(new[] { ("b", false), ("b", false) }, Searches());
    }

    [TestMethod]
    public void BitMenuButtonShouldSearchAStringTypedFurtherFromTheFocusedItem()
    {
        var com = RenderOpen();

        Type(com, Menu, "b");
        Type(com, Menu, "l");

        CollectionAssert.AreEqual(new[] { ("b", false), ("bl", true) }, Searches());
    }

    [TestMethod]
    [DataRow("en-US")]
    [DataRow("sv-SE")]
    public void BitMenuButtonShouldHandTheScriptTheCurrentCultureToMatchIn(string culture)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

        var com = RenderOpen();

        Type(com, Menu, "a");

        var search = Context.JSInterop.Invocations[FocusItem].Single(i => (string?)i.Arguments[1] == "char");
        Assert.AreEqual(culture, search.Arguments[5]);
    }

    [TestMethod]
    public void BitMenuButtonShouldNotHandACultureToTheMovesThatAreNoSearch()
    {
        var com = RenderOpen();

        Type(com, Menu, "ArrowDown");

        var move = Context.JSInterop.Invocations[FocusItem].Single(i => (string?)i.Arguments[1] == "next");
        Assert.IsNull(move.Arguments[5]);
    }

    [TestMethod]
    public void BitMenuButtonShouldStartTheStringOverInASubmenu()
    {
        var com = RenderOpen();

        Type(com, Menu, "s");
        com.FindAll($"{Menu} > ul > li > .bit-mnb-itm")[3].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Type(com, ".bit-mnb-sub", "e");

        // A string typed into one menu has nothing to do with the one beside it.
        CollectionAssert.AreEqual(new[] { ("s", false), ("e", false) }, Searches());
    }

    [TestMethod]
    public void BitMenuButtonShouldStartTheStringOverOnceTheMenuIsClosed()
    {
        var com = RenderOpen();

        Type(com, Menu, "b");
        Type(com, Menu, "Escape");
        com.Find(".bit-mnb-opb").Click();
        Type(com, Menu, "l");

        CollectionAssert.AreEqual(new[] { ("b", false), ("l", false) }, Searches());
    }
}
