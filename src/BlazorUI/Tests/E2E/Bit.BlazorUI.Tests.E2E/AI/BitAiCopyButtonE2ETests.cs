using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiCopyButton at /e2e/aicopybutton in a real browser: the text put on the clipboard (read back where the
/// engine lets a page read it), the confirmation in the glyph, the name and a polite live region, and its end after the
/// ConfirmationDuration; the legacy copy where the Clipboard API is missing, a copy that fails confirming nothing, a
/// missing Content and Disabled copying nothing, Label and ShowLabel, the keyboard, the AI glyph font, the Styles/Classes
/// and the --bit-Button-* variables reaching the button, and right-to-left.
/// </summary>
[TestClass]
public class BitAiCopyButtonE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aicopybutton";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Button(string id) => Page.Locator($"#{id} button.bit-btn");

    private ILocator Icon(string id) => Page.Locator($"#{id} i.bit-btn-icn");

    private ILocator Live(string id) => Page.Locator($"#{id} .bit-aicpb-liv");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    // Records what reaches the clipboard through either path, beside the real copy, in every engine.
    private const string ClipboardSpy =
        """
        (() => {
            window.__e2eCopied = [];
            const clipboard = navigator.clipboard;
            if (clipboard && clipboard.writeText) {
                const write = clipboard.writeText.bind(clipboard);
                clipboard.writeText = text => { window.__e2eCopied.push(text); return write(text); };
            }
            const exec = document.execCommand.bind(document);
            document.execCommand = (command, ...rest) => {
                if (command === 'copy') window.__e2eCopied.push(document.activeElement?.value ?? '');
                return exec(command, ...rest);
            };
        })();
        """;

    [TestMethod]
    public async Task APress_PutsTheContentOnTheClipboard_AndConfirmsIt()
    {
        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await Page.AddInitScriptAsync(ClipboardSpy);
        await OpenAsync(Route);

        var button = Button("cpb-basic");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Copy");
        await Expect(button).ToHaveAttributeAsync("title", "Copy");
        await Expect(Live("cpb-basic")).ToHaveTextAsync("");

        await button.ClickAsync();

        await Expect(Text("cpb-basic-count")).ToHaveTextAsync("1");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Copied");
        await Expect(button).ToHaveAttributeAsync("title", "Copied");
        await Expect(Icon("cpb-basic")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Icon("cpb-basic")).Not.ToHaveClassAsync(HasClass("bit-icon-ai--Copy"));
        await Expect(Live("cpb-basic")).ToHaveTextAsync("Copied");

        var copied = await Page.EvaluateAsync<string[]>("() => window.__e2eCopied");
        CollectionAssert.Contains(copied, "Hello from the clipboard");

        if (IsChromium)
        {
            Assert.AreEqual("Hello from the clipboard", await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));

            // And it pastes.
            await Page.Locator("#cpb-paste-target").FocusAsync();
            await Page.Keyboard.PressAsync("ControlOrMeta+V");
            await Expect(Page.Locator("#cpb-paste-target")).ToHaveValueAsync("Hello from the clipboard");
        }
    }

    [TestMethod]
    public async Task TheConfirmation_IsAnnouncedPolitely_AndEndsAfterTheConfirmationDuration()
    {
        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await OpenAsync(Route);

        var live = Live("cpb-short");
        await Expect(live).ToHaveAttributeAsync("role", "status");
        await Expect(live).ToHaveAttributeAsync("aria-live", "polite");

        await Button("cpb-short").ClickAsync();
        await Expect(live).ToHaveTextAsync("Copied");
        await Expect(Button("cpb-short")).ToHaveAttributeAsync("aria-label", "Copied");

        // The region stays on the page with nothing in it, ready to announce the next copy.
        await Expect(Button("cpb-short")).ToHaveAttributeAsync("aria-label", "Copy", new() { Timeout = 5000 });
        await Expect(live).ToHaveTextAsync("");
        await Expect(live).ToHaveCountAsync(1);
        await Icon("cpb-short").AssertAiGlyphAsync("Copy");

        // A copy during the confirmation starts it over.
        await Button("cpb-short").ClickAsync();
        await Expect(Text("cpb-short-count")).ToHaveTextAsync("2");
        await Expect(Button("cpb-short")).ToHaveAttributeAsync("aria-label", "Copied");
        await Button("cpb-short").ClickAsync();
        await Expect(Text("cpb-short-count")).ToHaveTextAsync("3");
        await Expect(Button("cpb-short")).ToHaveAttributeAsync("aria-label", "Copy", new() { Timeout = 5000 });
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task WithoutTheClipboardApi_TheLegacyCopyStillCopies_AndKeepsTheFocusOnTheButton()
    {
        // The copy runs once the press has made the round trip to .NET, after the gesture, which the legacy command of
        // WebKit refuses (the Clipboard API, which the copy prefers, takes it).
        if (IsWebKit) Assert.Inconclusive("WebKit refuses document.execCommand('copy') outside the user's own gesture.");

        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await Page.AddInitScriptAsync("Object.defineProperty(Navigator.prototype, 'clipboard', { get: () => undefined, configurable: true });");
        await Page.AddInitScriptAsync(ClipboardSpy);
        await OpenAsync(Route);

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => navigator.clipboard === undefined"));

        var scrolled = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Button("cpb-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Text("cpb-basic-count")).ToHaveTextAsync("1");
        await Expect(Button("cpb-basic")).ToHaveAttributeAsync("aria-label", "Copied");
        CollectionAssert.Contains(await Page.EvaluateAsync<string[]>("() => window.__e2eCopied"), "Hello from the clipboard");

        // The hidden textarea it copies through is gone, the focus is back on the button, and the page did not move.
        await Expect(Button("cpb-basic")).ToBeFocusedAsync();
        await Expect(Page.Locator("body > textarea[readonly][aria-hidden=true]")).ToHaveCountAsync(0);
        Assert.AreEqual(scrolled, await Page.EvaluateAsync<double>("() => window.scrollY"), 1);
    }

    [TestMethod]
    public async Task ACopyThatFails_ConfirmsNothing()
    {
        // The Clipboard API refuses and the legacy command fails, until the test lets the legacy one work.
        await Page.AddInitScriptAsync(
            """
            (() => {
                window.__e2eCopyAttempts = 0;
                window.__e2eCopyWorks = false;
                if (navigator.clipboard) navigator.clipboard.writeText = () => Promise.reject(new DOMException('Denied', 'NotAllowedError'));
                document.execCommand = command => { if (command === 'copy') window.__e2eCopyAttempts++; return window.__e2eCopyWorks; };
            })();
            """);
        await OpenAsync(Route);

        await Button("cpb-basic").ClickAsync();
        await Page.WaitForFunctionAsync("() => window.__e2eCopyAttempts === 1");

        // A copy that works on another button, after it, proves the first press was answered.
        await Page.EvaluateAsync("() => { window.__e2eCopyWorks = true; }");
        await Button("cpb-kb").ClickAsync();
        await Expect(Text("cpb-kb-count")).ToHaveTextAsync("1");

        await Expect(Text("cpb-basic-count")).ToHaveTextAsync("0");
        await Expect(Button("cpb-basic")).ToHaveAttributeAsync("aria-label", "Copy");
        await Expect(Live("cpb-basic")).ToHaveTextAsync("");
        await Icon("cpb-basic").AssertAiGlyphAsync("Copy");
    }

    [TestMethod]
    public async Task NoContentOrDisabled_TheButtonIsUnavailable_AndCopiesNothing()
    {
        await Page.AddInitScriptAsync(ClipboardSpy);
        await OpenAsync(Route);

        foreach (var id in new[] { "cpb-null", "cpb-dis" })
        {
            await Expect(Button(id)).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(Button(id)).ToHaveClassAsync(HasClass("bit-dis"));
            await Button(id).ClickAsync(new() { Force = true });
            await Button(id).DispatchEventAsync("click");
        }

        await Expect(Root("cpb-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Root("cpb-null")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        // A copy that works, after them, proves the presses were answered.
        await Button("cpb-kb").ClickAsync();
        await Expect(Text("cpb-kb-count")).ToHaveTextAsync("1");
        await Expect(Text("cpb-null-count")).ToHaveTextAsync("0");
        await Expect(Text("cpb-dis-count")).ToHaveTextAsync("0");
        CollectionAssert.AreEqual(new[] { "Copied with the keyboard" }, (await Page.EvaluateAsync<string[]>("() => window.__e2eCopied")).Distinct().ToArray());
    }

    [TestMethod]
    public async Task LabelAndShowLabel_NameTheButton_AndShowTheNameBesideTheIcon()
    {
        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await OpenAsync(Route);

        await Expect(Button("cpb-label")).ToHaveTextAsync("Copy");
        await Expect(Button("cpb-custom-label")).ToHaveTextAsync("Copy code");
        await Expect(Button("cpb-custom-label")).ToHaveAttributeAsync("aria-label", "Copy code");
        await Expect(Button("cpb-icon-only")).ToHaveTextAsync("");
        await Expect(Button("cpb-icon-only")).ToHaveAttributeAsync("aria-label", "Copy the answer");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Copy the answer", Exact = true })).ToHaveCountAsync(1);

        // The shown name follows the confirmation too.
        await Button("cpb-custom-label").ClickAsync();
        await Expect(Button("cpb-custom-label")).ToHaveTextAsync("Copied");
        await Expect(Button("cpb-custom-label")).ToHaveAttributeAsync("aria-label", "Copied");

        // The icon and the label sit side by side.
        var icon = await Icon("cpb-label").RectAsync();
        var label = await Page.Locator("#cpb-label .bit-btn-prt").RectAsync();
        Assert.IsTrue(icon.Right <= label.Left + 1, "The label follows the icon.");
    }

    [TestMethod]
    public async Task Keyboard_TabReachesTheButton_AndEnterOrSpaceCopies()
    {
        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await OpenAsync(Route);

        await Page.Locator("#cpb-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Button("cpb-kb")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("cpb-kb-count")).ToHaveTextAsync("1");
        await Expect(Button("cpb-kb")).ToHaveAttributeAsync("aria-label", "Copied");
        await Expect(Button("cpb-kb")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(Text("cpb-kb-count")).ToHaveTextAsync("2");

        if (IsChromium) Assert.AreEqual("Copied with the keyboard", await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
    }

    [TestMethod]
    public async Task Glyph_TheCopyIconIsDrawnFromTheAiFont_AndTheCheckMarkFromTheCoreOne()
    {
        if (IsChromium) await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await OpenAsync(Route);

        await Icon("cpb-basic").AssertAiGlyphAsync("Copy");
        Assert.IsTrue(await Page.AiGlyphFontLoadsAsync(""), "The AI font is fetched and draws the Copy glyph.");

        await Button("cpb-basic").ClickAsync();
        await Expect(Icon("cpb-basic")).ToHaveClassAsync(HasClass("bit-icon--Accept"));

        var (family, content) = await Icon("cpb-basic").GlyphAsync();
        Assert.IsFalse(family.Contains(AiE2EHelpers.AiGlyphFontFamily), "The check mark is a glyph of the core font.");
        Assert.IsFalse(content is "none" or "normal" or "\"\"" or "", $"The check mark draws a character, was {content}.");
    }

    [TestMethod]
    public async Task Style_TheStylesAndClasses_AndTheButtonsOwnVariables_ReachTheButton()
    {
        await OpenAsync(Route);

        await Expect(Root("cpb-style")).ToHaveClassAsync(HasClass("cpb-custom-root"));
        await Expect(Button("cpb-style")).ToHaveClassAsync(HasClass("cpb-custom-button"));
        await Expect(Button("cpb-style")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Icon("cpb-style")).ToHaveClassAsync(HasClass("cpb-custom-icon"));
        await Icon("cpb-style").AssertAiGlyphAsync("Copy");

        await Expect(Button("cpb-button-var")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        // A neutral text button by default, and the live region out of sight.
        await Expect(Button("cpb-basic")).ToHaveClassAsync(HasClass("bit-btn-txt"));
        await Expect(Button("cpb-basic")).ToHaveClassAsync(HasClass("bit-btn-dft-ter"));
        await Expect(Root("cpb-basic")).ToHaveCSSAsync("display", "inline-flex");
        var live = await Live("cpb-basic").BoundingBoxAsync();
        Assert.IsTrue(live is null || live.Width <= 1, "The live region is visually hidden.");
    }

    [TestMethod]
    public async Task Rtl_TheLabelLeadsFromTheRight()
    {
        await OpenAsync(Route);

        await Expect(Root("cpb-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Button("cpb-rtl")).ToHaveCSSAsync("direction", "rtl");

        var icon = await Icon("cpb-rtl").RectAsync();
        var label = await Page.Locator("#cpb-rtl .bit-btn-prt").RectAsync();
        Assert.IsTrue(icon.Left >= label.Right - 1, "Right to left, the icon is on the right of the label.");
        await Expect(Root("cpb-basic")).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
    }
}
