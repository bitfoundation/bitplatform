using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the overlay of BitModal, driven with real pointer and keyboard input.
///
/// A press on the overlay is left its default action so that it blurs the input the user was typing into:
/// the input only commits what was typed once it loses the focus, and that has to happen before the click
/// the press turns into reaches OnOverlayClick and OnDismiss. bUnit cannot see that ordering - it has no
/// focus and no default actions - so the behavior is pinned down here, in a real browser.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[Ignore("Browser tests must be run explicitly. Use: dotnet test --filter FullyQualifiedName~BitModalBrowserTests")]
public class BitModalBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task BitModal_OverlayPress_CommitsTheTypedValueBeforeTheDismissalHandlersRun()
    {
        const string typed = "committed";

        await Page.GotoAsync($"{BaseUrl}/regression/modal-overlay-commit");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        await Page.Locator("#btn-open").ClickAsync();
        await WaitForStatus("Open");

        // The Modal puts the focus on its first focusable element as it opens; typing before that lands
        // would have the focus move out from under the keyboard.
        var input = Page.Locator(".bit-mdl input");
        await Expect(input).ToBeFocusedAsync();

        await Page.Keyboard.TypeAsync(typed);

        // The corner of the page is covered by the overlay and by nothing else of the Modal, whose content
        // is centered. The mouse is moved there and pressed, as a user would, rather than the click being
        // dispatched on the element.
        await Page.Mouse.ClickAsync(5, 5);
        await WaitForStatus("Dismissed");

        await Expect(Page.Locator("#overlay-click-value")).ToHaveTextAsync(typed);
        await Expect(Page.Locator("#dismiss-value")).ToHaveTextAsync(typed);
    }
}
