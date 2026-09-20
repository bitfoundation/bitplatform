using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the AutoHeight mode of BitTextField, driven with real pointer and keyboard input.
///
/// Measuring the content collapses the textarea for a moment, and Firefox and WebKit clamp the scroll position of
/// the containers around it to that shorter content. A press on a button below the field blurs it, the change
/// re-renders it and it is measured again while the button is still held down - so without the scroll positions
/// being put back the button moves away and the click is lost. Chromium does not clamp, so these tests only fail
/// there if the measuring itself breaks: run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitTextFieldBrowserTests
public class BitTextFieldBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task BitTextField_AutoHeight_PressBelowTheFieldWhileScrolledToTheEnd_ReachesTheButton()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/textfield-autoheight-scroll");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        var textarea = Page.Locator("#scroller textarea.bit-tfl-inp");

        // More lines than the rows attribute holds: only then does the collapse shrink the field at all.
        await textarea.ClickAsync();
        await TypeLines(5);

        // Typing measures the field as well, so the container is scrolled to its end again right before the press.
        await ScrollToEnd();

        var button = await Page.Locator("#btn-save").BoundingBoxAsync();
        Assert.IsNotNull(button);

        // A human press, long enough for the change round trip to re-measure the field before the mouse is released.
        await Page.Mouse.ClickAsync(button.X + button.Width / 2, button.Y + button.Height / 2, new() { Delay = 300 });

        // A lost click never saves, so there is nothing to wait for beyond the round trip of one that landed.
        await Expect(Page.Locator("#save-count")).ToHaveTextAsync("1", new() { Timeout = 5000 });
    }

    [TestMethod]
    public async Task BitTextField_AutoHeight_DeletingLines_ShrinksTheField()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/textfield-autoheight-scroll");
        await WaitForStatus("Ready");

        var textarea = Page.Locator("#scroller textarea.bit-tfl-inp");

        await textarea.ClickAsync();
        await TypeLines(6);
        await ScrollToEnd();

        var grown = await textarea.EvaluateAsync<double>("el => el.offsetHeight");

        // "line 6" and the line break before it, four times over.
        for (var i = 0; i < 4 * 7; i++)
        {
            await Page.Keyboard.PressAsync("Backspace");
        }

        await Page.WaitForFunctionAsync("([el, grown]) => el.offsetHeight < grown", new object[] { await textarea.ElementHandleAsync(), grown },
            new PageWaitForFunctionOptions { Timeout = DefaultTimeout });
    }

    private async Task TypeLines(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            if (i > 1) await Page.Keyboard.PressAsync("Enter");

            await Page.Keyboard.TypeAsync($"line {i}");
        }
    }

    private Task ScrollToEnd()
    {
        return Page.EvaluateAsync(@"() => new Promise(resolve => {
            const scroller = document.getElementById('scroller');
            scroller.scrollTop = scroller.scrollHeight;
            requestAnimationFrame(() => requestAnimationFrame(resolve));
        })");
    }
}
