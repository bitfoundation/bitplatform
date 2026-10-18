using Bit.BlazorUI.Tests.Trimming.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazorUI.Tests.Trimming;

/// <summary>
/// The trimming a Blazor WebAssembly app gets by default: Bit.BlazorUI.Tests.Consumer, published trimmed, its bundles put
/// back together off what ILLink left in the trimmed library assemblies - and, run in every engine, working.
/// </summary>
[TestClass]
public sealed class WebAssemblyTrimmingTests
{
    private const string Core = "Bit.BlazorUI";
    private const string Extras = "Bit.BlazorUI.Extras";

    private static PublishResult Published => Publisher.Publish(Publisher.Consumer);

    [TestMethod]
    public void AWebAssemblyAppShouldBeTrimmedByDefault()
    {
        Published.AssertSucceeded();

        // The app renders some two dozen components; the full bundles hold every one of them.
        var styles = Published.KeptStyles(Core);
        Assert.IsTrue(styles.Count is > 10 and < 60, $"The trimmed core stylesheet keeps {styles.Count} components: {string.Join(", ", styles)}");
        foreach (var expected in new[] { "BitButton", "BitTextField", "BitDropdown", "BitDatePicker", "BitNav", "BitModal" }) CollectionAssert.Contains(styles, expected);
        foreach (var unexpected in new[] { "BitCard", "BitCarousel", "BitSplitter", "BitScrollablePane" }) CollectionAssert.DoesNotContain(styles, unexpected);

        var scripts = Published.KeptScripts(Core);
        foreach (var expected in new[] { "general", "Utils.Keys", "Callouts", "BitTextField", "BitNav" }) CollectionAssert.Contains(scripts, expected);
        foreach (var unexpected in new[] { "BitScrollablePane", "BitSplitter", "BitPullToRefresh" }) CollectionAssert.DoesNotContain(scripts, unexpected);

        CollectionAssert.Contains(Published.KeptStyles(Extras), "BitDataGrid");
        CollectionAssert.DoesNotContain(Published.KeptStyles(Extras), "BitPdfViewer");
        CollectionAssert.DoesNotContain(Published.KeptScripts(Extras), "BitPdfViewer");

        foreach (var package in new[] { Core, Extras })
        {
            Published.AssertScriptsAreThePiecesInOrder(package);
            Published.AssertStylesAreThePiecesInOrder(package);
        }
    }

    [TestMethod]
    [DynamicData(nameof(BrowserSession.Engines), typeof(BrowserSession))]
    public async Task ATrimmedAppShouldRunWithEveryComponentStyledAndNoScriptMissing(string engine)
    {
        await using var session = await BrowserSession.OpenAsync(engine);
        Published.AssertSucceeded();

        await using var server = await AppServer.StartStaticAsync(Published.WebRoot);
        var page = session.Page;

        await page.GotoAsync(server.Url);
        await page.WaitForSelectorAsync(".bit-btn", new() { Timeout = 90_000 });

        // What was trimmed away is gone from the page, and what the app uses is there.
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.Callouts.toggle === 'function' && typeof BitBlazorUI.Utils.claimEscape === 'function'"));
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof BitBlazorUI.ScrollablePane === 'undefined' && typeof BitBlazorUI.Splitter === 'undefined'"));

        await session.AssertEveryComponentIsStyledAsync(Published, Core, Extras);

        // The components whose scripts the trimming had to keep for them, each doing what it relies on its scripts for.
        // A callout - the script with the most moving parts of all - opens and closes on Escape, from each kind of field.
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-drp").First, "BitDropdown");
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-dtp input").First, "BitDatePicker");
        await session.OpenAndEscapeCalloutAsync(page.Locator(".bit-mnb button").First, "BitMenuButton");

        // A tag typed into the tags input and committed with Enter (BitTagsInput.ts).
        await page.Locator(".bit-tgi input").First.FillAsync("alpha");
        await page.Locator(".bit-tgi input").First.PressAsync("Enter");
        await session.WaitForAsync("() => document.querySelectorAll('.bit-tgi-tag').length === 1", "BitTagsInput did not add the tag typed into it");

        // Text typed into the rich text editor's surface (BitRichTextEditor.ts, of the Extras).
        await page.Locator(".bit-rte-edt").First.ClickAsync();
        await page.Keyboard.TypeAsync("hello");
        await session.WaitForAsync("() => document.querySelector('.bit-rte-edt').textContent.includes('hello')", "BitRichTextEditor did not take the text typed into it");

        // The theme switcher flips the document's theme (BitTheme.ts) and back. From the keyboard: the app's nav panel,
        // fixed to the side of the page, lies over the controls further down it.
        await PressAsync(page.Locator(".bit-ths-lgt").First);
        await session.WaitForAsync("() => document.documentElement.getAttribute('bit-theme') === 'dark'", "BitThemeSwitcher did not turn the page dark");
        await PressAsync(page.Locator(".bit-ths-drk").First);
        await session.WaitForAsync("() => document.documentElement.getAttribute('bit-theme') === 'light'", "BitThemeSwitcher did not turn the page light again");

        // A modal opened by a button, and closed on Escape by the surfaces' script.
        await PressAsync(page.GetByRole(AriaRole.Button, new() { Name = "Open", Exact = true }).First);
        await session.WaitForAsync("() => !!document.querySelector('.bit-mdl')", "BitModal did not open");
        await page.Keyboard.PressAsync("Escape");
        await session.WaitForAsync("() => !document.querySelector('.bit-mdl')", "Escape did not close the BitModal");

        // The data grid's rows, read through the names of the app's own record.
        await session.WaitForAsync("() => document.querySelector('.bit-dtg')?.textContent.includes('Ada')", "BitDataGrid rendered no rows");

        session.AssertNoErrors($"The trimmed app in {engine}");
    }

    private static async Task PressAsync(ILocator button)
    {
        await button.FocusAsync();
        await button.PressAsync("Enter");
    }
}
