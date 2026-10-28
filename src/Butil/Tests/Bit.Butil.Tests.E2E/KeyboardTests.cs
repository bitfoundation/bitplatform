using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Keyboard shortcuts and the keyboard event payload, driven by real key presses.
/// </summary>
/// <remarks>
/// Neither can be checked without a browser: whether a key still edits an input, and which element a
/// keydown went to, are both decided by the engine's own dispatch and focus handling.
/// </remarks>
[TestClass]
public class KeyboardTests : ButilPageTest
{
    [TestMethod]
    public async Task IgnoreWhenEditable_Leaves_An_Editing_Key_To_The_Input_Being_Typed_In()
    {
        await ClickAndExpectAsync("kb-editable", "kb:armed");

        // Focus is on the button that armed it, which takes no typing - so the shortcut fires.
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("kb:delete:1");

        // In the input the key keeps its own action, despite the shortcut's preventDefault: true.
        var input = Page.Locator("#kb-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Home");
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(input).ToHaveValueAsync("bc");

        // Back outside it, the next press is the second delivery, not the third: the one the input
        // took never reached .NET.
        await Page.Locator("#kb-editable").FocusAsync();
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("kb:delete:2");
    }

    [TestMethod]
    public async Task Keyboard_Event_Args_Report_The_Target_And_Whether_It_Takes_Typing()
    {
        await ClickAndExpectAsync("kb-target", "kb:target-armed");

        await Page.Locator("#kb-input").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("kb:target:ArrowRight/INPUT/True");

        await Page.Locator("#kb-target").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("kb:target:ArrowLeft/BUTTON/False");
    }
}
