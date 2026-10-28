using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Keyboard shortcuts and the keyboard event payload, driven by real key presses.
/// </summary>
/// <remarks>
/// Neither can be checked without a browser: whether a key still edits a control, and which element a
/// keydown went to, are both decided by the engine's own dispatch and focus handling. Every shortcut the
/// harness arms keeps <c>preventDefault: true</c>, so a control whose key was taken visibly stops changing.
/// A shortcut that stood aside is shown by the next press on <c>#kb-neutral</c> - focusable, but no control -
/// being counted as the first delivery rather than a later one.
/// </remarks>
[TestClass]
public class KeyboardTests : ButilPageTest
{
    private const string AttachOpenField = "host => host.attachShadow({ mode: 'open' }).innerHTML = '<input value=\"abc\">'";

    // A form-associated text field whose input sits behind a closed shadow root: from the page, a key typed into
    // it arrives with the host as its target, and the host's form association is all that can be seen of it.
    private const string AttachClosedField = """
        host => {
            if (!customElements.get('kb-closed-field')) {
                customElements.define('kb-closed-field', class extends HTMLElement {
                    static formAssociated = true;
                    constructor() {
                        super();
                        const root = this.attachShadow({ mode: 'closed' });
                        root.innerHTML = '<input value="abc">';
                        this.input = root.querySelector('input');
                    }
                });
            }
            host.appendChild(document.createElement('kb-closed-field'));
        }
        """;

    private ILocator Status => Page.Locator("#status");

    [TestMethod]
    public async Task A_Shortcut_Without_Modifiers_Leaves_Its_Key_To_A_Text_Field()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");

        await PlaceCaretAsync("#kb-input", 0);
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(Page.Locator("#kb-input")).ToHaveValueAsync("bc");

        await PlaceCaretAsync("#kb-textarea", 0);
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(Page.Locator("#kb-textarea")).ToHaveValueAsync("bc");

        await PlaceCaretAsync("#kb-rich", 0);
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(Page.Locator("#kb-rich")).ToHaveTextAsync("bc");

        await PressOnAsync("#kb-neutral", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Delete:1");
    }

    [TestMethod]
    public async Task A_Shortcut_Fires_On_A_Control_With_No_Use_For_Its_Key()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");

        // Read-only text has nothing to delete and no caret to move, and neither a select nor a checkbox does
        // anything with Delete.
        await PressOnAsync("#kb-readonly", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Delete:1");
        await PressOnAsync("#kb-readonly", "ArrowRight");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:ArrowRight:1");
        await PressOnAsync("#kb-select", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Delete:2");
        await PressOnAsync("#kb-checkbox", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Delete:3");

        await PressOnAsync("#kb-checkbox", "ArrowRight");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:ArrowRight:2");
    }

    [TestMethod]
    public async Task A_Shortcut_Stands_Aside_For_The_Keys_A_Control_Acts_On()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");

        await PressOnAsync("#kb-range", "ArrowRight");
        await Assertions.Expect(Page.Locator("#kb-range")).ToHaveValueAsync("6");

        await PressOnAsync("#kb-checkbox", "Space");
        await Assertions.Expect(Page.Locator("#kb-checkbox")).ToBeCheckedAsync();

        await PlaceCaretAsync("#kb-input", 0);
        await Page.Keyboard.PressAsync("ArrowRight");
        Assert.AreEqual(1, await Page.Locator("#kb-input").EvaluateAsync<int>("el => el.selectionStart"));

        await PressOnAsync("#kb-neutral", "ArrowRight");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:ArrowRight:1");
        await PressOnAsync("#kb-neutral", "Space");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Space:1");
    }

    [TestMethod]
    public async Task A_Modifier_Keeps_A_Shortcut_Live_In_A_Text_Field_Unless_It_Says_Otherwise()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");

        // Ctrl+Delete, on the defaults, fires from inside the input.
        await PressOnAsync("#kb-input", "Control+Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Ctrl+Delete:1");

        // Ctrl+Backspace, with IgnoreWhenEditable = true, stands aside there.
        await PressOnAsync("#kb-input", "Control+Backspace");
        await PressOnAsync("#kb-neutral", "Control+Backspace");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Ctrl+Backspace:1");

        // Backspace, with IgnoreWhenEditable = false, takes the key from the field: nothing is deleted.
        await PlaceCaretAsync("#kb-textarea", 3);
        await Page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Backspace:1");
        await Assertions.Expect(Page.Locator("#kb-textarea")).ToHaveValueAsync("abc");
    }

    [TestMethod]
    public async Task A_Shortcut_Scoped_To_A_Text_Field_Fires_While_The_User_Types_There()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");

        await PressOnAsync("#kb-scoped", "Enter");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:scoped-Enter:1");
    }

    [TestMethod]
    public async Task A_Shortcut_Sees_A_Text_Field_Through_An_Open_Shadow_Root()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");
        await Page.Locator("#kb-host").EvaluateAsync(AttachOpenField);

        // Playwright's CSS locators pierce open shadow roots.
        await PlaceCaretAsync("#kb-host input", 0);
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(Page.Locator("#kb-host input")).ToHaveValueAsync("bc");

        await PressOnAsync("#kb-neutral", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Delete:1");
    }

    [TestMethod]
    public async Task A_Shortcut_Reads_A_Closed_Shadow_Root_By_What_Its_Host_Declares()
    {
        await ClickAndExpectAsync("kb-arm", "kb:armed");
        await Page.Locator("#kb-host").EvaluateAsync(AttachClosedField);

        var field = Page.Locator("kb-closed-field");
        await field.EvaluateAsync("el => { el.input.focus(); el.input.setSelectionRange(0, 0); }");
        await Page.Keyboard.PressAsync("Delete");
        Assert.AreEqual("bc", await field.EvaluateAsync<string>("el => el.input.value"));

        await PressOnAsync("#kb-neutral", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:Delete:1");
    }

    [TestMethod]
    public async Task Keyboard_Event_Args_Report_The_Target_And_Whether_It_Takes_The_Key()
    {
        await ClickAndExpectAsync("kb-target", "kb:target-armed");

        await PressOnAsync("#kb-input", "ArrowRight");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:target:ArrowRight/INPUT/True");

        await PressOnAsync("#kb-select", "Delete");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:target:Delete/SELECT/False");

        await PressOnAsync("#kb-neutral", "ArrowLeft");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:target:ArrowLeft/DIV/False");

        // Through an open shadow root the payload names the input inside it, not the host.
        await Page.Locator("#kb-host").EvaluateAsync(AttachOpenField);
        await PressOnAsync("#kb-host input", "ArrowLeft");
        await Assertions.Expect(Status).ToHaveTextAsync("kb:target:ArrowLeft/INPUT/True");
    }

    private async Task PressOnAsync(string selector, string key)
    {
        await Page.Locator(selector).FocusAsync();
        await Page.Keyboard.PressAsync(key);
    }

    // Sets the caret directly rather than pressing Home, which macOS maps to scrolling the document, not to
    // moving the caret - so the same test reads the same on every platform.
    private Task PlaceCaretAsync(string selector, int offset)
        => Page.Locator(selector).EvaluateAsync("""
            (el, offset) => {
                el.focus();
                if (el.setSelectionRange) el.setSelectionRange(offset, offset);
                else getSelection().collapse(el.firstChild, offset);
            }
            """, offset);
}
