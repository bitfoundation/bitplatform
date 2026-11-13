using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitListBox (/e2e/listbox) in a real browser: the list as one tab stop whose arrow keys, Home, End
/// and typeahead move the focus from item to item - with the selection in single select mode, without it in multi
/// select mode where Space chooses - and never scroll the page; a long list keeping the item the keyboard is on in view;
/// an EditForm validating it; axe; and forced colors.
/// </summary>
[TestClass]
public class BitListBoxE2ETests : E2ETestBase
{
    private const string Url = "/e2e/listbox";

    private ILocator Options(string id) => Page.Locator($"#{id} [role=option]");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private Task<double> ScrollYAsync() => Page.EvaluateAsync<double>("() => window.scrollY");

    [TestMethod]
    public async Task TheList_IsOneTabStop_WhoseKeysMoveTheFocusAndTheSelection()
    {
        await OpenAsync(Url);

        await Page.Locator("#before-single").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Options("lbx-single").Nth(0)).ToBeFocusedAsync();

        // The arrows move the focus and, with one to choose, the selection - over the disabled Banana.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Options("lbx-single").Nth(1)).ToBeFocusedAsync();
        await Expect(Text("single-value")).ToHaveTextAsync("avocado");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Options("lbx-single").Nth(3)).ToBeFocusedAsync();
        await Expect(Options("lbx-single").Nth(3)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Text("single-value")).ToHaveTextAsync("blueberry");

        await Page.Keyboard.PressAsync("End");
        await Expect(Text("single-value")).ToHaveTextAsync("cherry");
        await Page.Keyboard.PressAsync("Home");
        await Expect(Text("single-value")).ToHaveTextAsync("apple");

        // Typing the start of a text goes to it.
        await Page.Keyboard.PressAsync("c");
        await Expect(Options("lbx-single").Nth(4)).ToBeFocusedAsync();
        await Expect(Text("single-value")).ToHaveTextAsync("cherry");

        // The list is one stop: Tab leaves it from any item, not only from the last, and Shift+Tab comes back to the
        // selected item.
        await Page.Keyboard.PressAsync("Home");
        await Expect(Options("lbx-single").Nth(0)).ToBeFocusedAsync();
        // The tab stop has moved along with the focus by the time a person presses Tab; WebKit can show the focus a
        // moment ahead of it when keys follow each other within milliseconds, so the test waits for it as a person would.
        await Expect(Options("lbx-single").Nth(0)).ToHaveAttributeAsync("tabindex", "0");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-single")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Options("lbx-single").Nth(0)).ToBeFocusedAsync();
        await Expect(Options("lbx-single").Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task TheKeysOfTheList_DoNotScrollThePage()
    {
        await OpenAsync(Url);

        await Options("lbx-multi").Nth(0).FocusAsync();
        var before = await ScrollYAsync();

        // Each of the keys that scroll a page down by default is checked on its own: Home, which scrolls it back up,
        // would hide a scroll the others did.
        async Task PressAndStayAsync(string key)
        {
            await Page.Keyboard.PressAsync(key);
            await NextFramesAsync();
            Assert.AreEqual(before, await ScrollYAsync(), 1, $"the page stayed where it was on {key}.");
        }

        await PressAndStayAsync("ArrowDown");
        await Expect(Options("lbx-multi").Nth(1)).ToBeFocusedAsync();
        await PressAndStayAsync("Space");
        await Expect(Text("multi-values")).ToHaveTextAsync("avocado");
        await PressAndStayAsync("End");
        await Expect(Options("lbx-multi").Nth(4)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AMultiSelectList_MovesWithTheArrows_AndChoosesWithSpaceOrAClick()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#lbx-multi [role=listbox]")).ToHaveAttributeAsync("aria-multiselectable", "true");

        await Options("lbx-multi").Nth(0).ClickAsync();
        await Expect(Text("multi-values")).ToHaveTextAsync("apple");

        // Moving only moves.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Options("lbx-multi").Nth(1)).ToBeFocusedAsync();
        await Expect(Text("multi-values")).ToHaveTextAsync("apple");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Text("multi-values")).ToHaveTextAsync("apple,avocado");
        await Expect(Page.Locator("#lbx-multi .bit-lbx-sel .bit-lbx-chk i")).ToHaveCountAsync(2);

        await Options("lbx-multi").Nth(0).ClickAsync();
        await Expect(Text("multi-values")).ToHaveTextAsync("avocado");
    }

    [TestMethod]
    public async Task ALongList_KeepsTheItemTheKeyboardIsOnInView()
    {
        await OpenAsync(Url);

        var list = Page.Locator("#lbx-long [role=listbox]");

        await Options("lbx-long").Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("End");

        await Expect(Text("long-value")).ToHaveTextAsync("30");
        await Expect(Options("lbx-long").Nth(29)).ToBeFocusedAsync();

        var listBox = (await list.BoundingBoxAsync())!;
        var last = (await Options("lbx-long").Nth(29).BoundingBoxAsync())!;

        Assert.IsTrue(listBox.Height < 8 * 16 + 4, $"the list is capped by MaxHeight ({listBox.Height}px).");
        Assert.IsTrue(last.Y >= listBox.Y - 1 && last.Y + last.Height <= listBox.Y + listBox.Height + 1, "the last item is scrolled into view.");
    }

    [TestMethod]
    public async Task AnEditForm_ValidatesTheList()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Page.Locator("#lbx-form .bit-lbx-err")).ToHaveTextAsync("Pick a fruit");
        await Expect(Page.Locator("#lbx-form [role=listbox]")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#lbx-form [role=listbox]")).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Text("form-submitted")).ToHaveTextAsync("False");

        await Options("lbx-form").Nth(3).ClickAsync();
        await Expect(Page.Locator("#lbx-form .bit-lbx-err")).ToHaveCountAsync(0);

        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Text("form-submitted")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task TheLists_HaveNoAxeViolations()
    {
        await OpenAsync(Url);

        await Options("lbx-single").Nth(1).ClickAsync();
        await Options("lbx-multi").Nth(0).ClickAsync();

        await Expect(Page.Locator("#lbx-single [role=listbox]")).ToHaveAccessibleNameAsync("Fruit");

        await AssertNoAxeViolationsAsync("#lbx-single");
        await AssertNoAxeViolationsAsync("#lbx-multi");
        await AssertNoAxeViolationsAsync("#lbx-long");
    }

    [TestMethod]
    public async Task ForcedColors_DrawTheSelectedItemInHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Url);
        await Options("lbx-single").Nth(0).ClickAsync();
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        await Expect(Options("lbx-single").Nth(0)).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("Highlight"));
        await Expect(Options("lbx-single").Nth(2)).ToHaveCSSAsync("color", await Page.ResolveColorAsync("GrayText"));
    }
}
