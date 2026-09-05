using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The recent-platform wrappers: PWA hooks, the CSS Typed OM, custom-element state, invoker
/// commands, <c>moveBefore</c>, text fragments, close watchers, the Web Animations additions and the
/// built-in AI availability probes.
/// </summary>
/// <remarks>
/// The suite runs on Chromium, which is the only engine that implements most of this - so the
/// assertions can be exact rather than "either answer is fine". Nothing here downloads a model,
/// prompts, or needs an installed app: the AI checks stop at the availability probe, and the PWA
/// checks stop at support detection, which is all a browser tab can reach.
/// </remarks>
[TestClass]
public class PlatformApisTests : ButilPageTest
{
    [TestMethod]
    public async Task Support_Probes_All_Answer_Through_Real_Interop()
    {
        // 16 probes answered. How many are true differs per engine and per build, so the check is
        // that every one returned at all - a mistyped BitButil.<module>.<function> identifier would
        // throw instead of answering.
        await ClickAndExpectAsync("plat-support", "plat:support:16/");
    }

    [TestMethod]
    public async Task Css_Supports_Escape_And_The_Style_Maps_Round_Trip()
    {
        // supports(display,grid)=True, supports(display,butil-nope)=False, the escaped identifier,
        // the write=True, the value read back as 120, and the computed unit "px".
        await ClickAndExpectAsync("plat-css", "plat:css:True/False/\\31 st\\:item/True/120/px");
    }

    [TestMethod]
    public async Task CustomElements_Define_Then_Toggle_A_State()
    {
        // defined, state added, state present, one state listed, cleared, and gone afterwards.
        await ClickAndExpectAsync("plat-custom", "plat:custom:True/True/True/1/True/False");
    }

    [TestMethod]
    public async Task InvokerCommands_Wires_A_Button_And_Delivers_Its_Command()
    {
        // Wiring first: nothing has been clicked, so no command has arrived yet.
        await ClickAndExpectAsync("plat-command", "plat:command:True/--butil-e2e/0");

        // The browser dispatches the command at the target; the subscription counts it.
        await Page.Locator("#plat-command-invoker").ClickAsync();
        await ClickAndExpectAsync("plat-command", "plat:command:True/--butil-e2e/1");
    }

    [TestMethod]
    public async Task MoveBefore_Moves_The_Node_Without_Reparenting_Through_The_Document()
    {
        // supported, moved, one child in the target and none left in the source.
        await ClickAndExpectAsync("plat-move", "plat:move:True/True/1/0");
    }

    [TestMethod]
    public async Task TextFragment_Builds_A_Directive_And_A_Url()
    {
        // The encoded directive, then the url it produces (which starts with its own "/e2e", hence
        // the doubled separator), then the 1 directive parsed back out of that url.
        await ClickAndExpectAsync("plat-fragment",
            "plat:fragment::~:text=a%20distinctive%20phrase//e2e#:~:text=a%20distinctive%20phrase/1");
    }

    [TestMethod]
    public async Task CloseWatcher_Close_Fires_The_Handler()
    {
        await ClickAndExpectAsync("plat-close", "plat:close:True");
    }

    [TestMethod]
    public async Task Animations_Are_Listed_Committed_And_Cancelled()
    {
        // Scroll-driven timelines are supported on Chromium, one animation was running when asked,
        // its time came back in milliseconds (a scroll-driven one would say percent), commitStyles
        // succeeded, and CancelAnimations found none left after Cancel.
        await ClickAndExpectAsync("plat-animations", "plat:animations:True/1/ms/True/0");
    }

    [TestMethod]
    public async Task ComposedRanges_Reports_The_Selection()
    {
        // Supported on Chromium, and one range for the element the harness selected.
        await ClickAndExpectAsync("plat-composed", "plat:composed:True/1");
    }

    [TestMethod]
    public async Task BuiltInAi_Availability_Answers_Without_Creating_A_Session()
    {
        // Headless Chromium has no on-device model, so the probes answer Unavailable rather than
        // throwing - which is the contract every caller is told to branch on. Whether the API object
        // itself exists depends on the build, so only the two availability answers are pinned.
        await ClickAndExpectAsync("plat-ai", "plat:ai:");
        StringAssert.Contains(await CurrentStatusAsync(), "/Unavailable/Unavailable/",
            "both availability probes must answer Unavailable where no model can be had");
    }
}
