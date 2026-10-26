using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Bit.BlazorUI.Tests.Trimming.Infrastructure;

/// <summary>
/// A published app open in one of Playwright's engines, with everything the page reports going wrong collected as it
/// runs: an error on the console, an uncaught exception (a <c>BitBlazorUI.x is undefined</c> of a script the trimming
/// left out), a request the publish cannot answer.
/// </summary>
public sealed partial class BrowserSession : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;

    private BrowserSession(IPlaywright playwright, IBrowser browser, IPage page)
    {
        _playwright = playwright;
        _browser = browser;
        Page = page;

        page.Console += (_, message) => { if (message.Type == "error" && IsFavicon(message.Location) is false && IsFavicon(message.Text) is false) Errors.Add($"console: {message.Text}"); };
        page.PageError += (_, error) => { if (IsResizeObserverLoop(error) is false) Errors.Add($"page: {error}"); };
        page.Response += (_, response) => { if (response.Status >= 400 && IsFavicon(response.Url) is false) Errors.Add($"{response.Status}: {response.Url}"); };
    }

    // The icon an engine asks every page for whether it names one or not: none of the host pages does, and the 404 it gets
    // back (and the console line some engines log for it) says nothing about the trimming.
    private static bool IsFavicon(string? text) => text?.Contains("/favicon.ico", StringComparison.OrdinalIgnoreCase) is true;

    // The notice an engine raises when a ResizeObserver's callback resizes what it observes and the rest of its notifications
    // wait for the next frame. Nothing failed, though WebKit reports it as an uncaught error (Belemetry ignores it by default
    // for the same reason), and it says nothing about the trimming.
    private static bool IsResizeObserverLoop(string? text) => text?.Contains("ResizeObserver loop", StringComparison.Ordinal) is true;

    /// <summary>The engines a trimmed app is run in: every one Playwright drives.</summary>
    public static IEnumerable<object[]> Engines => [["chromium"], ["firefox"], ["webkit"]];

    public IPage Page { get; }

    public List<string> Errors { get; } = [];

    /// <summary>Opens a page in the engine, or leaves the test inconclusive where the browser tests are not asked for.</summary>
    public static async Task<BrowserSession> OpenAsync(string engine)
    {
        if (Environment.GetEnvironmentVariable("RUN_BROWSER_TESTS") != "1") Assert.Inconclusive("Set RUN_BROWSER_TESTS=1 to run the trimmed apps in the browsers.");

        var playwright = await Playwright.CreateAsync();
        var type = engine switch
        {
            "chromium" => playwright.Chromium,
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "Not an engine Playwright drives."),
        };

        var browser = await type.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
        context.SetDefaultTimeout(30_000);
        return new BrowserSession(playwright, browser, await context.NewPageAsync());
    }

    public void AssertNoErrors(string what) => Assert.AreEqual(0, Errors.Count, $"{what} reported errors: {string.Join(" | ", Errors)}");

    /// <summary>Opens a callout through the given trigger and closes it again with Escape, as the user would.</summary>
    public async Task OpenAndEscapeCalloutAsync(ILocator trigger, string what)
    {
        await trigger.ClickAsync();
        await WaitForAsync("() => BitBlazorUI.Callouts.current.calloutId !== ''", $"{what} opened no callout");

        await Page.Keyboard.PressAsync("Escape");
        await WaitForAsync("() => BitBlazorUI.Callouts.current.calloutId === ''", $"Escape did not close the callout of {what}");
    }

    /// <summary>
    /// The AI package's copy button (BitAiCopyButton), drawn and pressed: its glyph is drawn with the package's own font -
    /// published, served and loaded - rather than an empty box, and a press reaches its script, which a lazy app imports
    /// on that first call. Whether the browser lets the copy reach the clipboard is the engine's business.
    /// </summary>
    public async Task AssertAiCopyButtonWorksAsync()
    {
        var glyph = await Page.EvaluateAsync<string[]>(@"async () => {
            const icon = document.querySelector('.bit-aicpb .bit-icon-ai--Copy');
            if (!icon) return ['', '', '0'];
            const faces = await document.fonts.load('16px ""Fabric MDL2 bit BlazorUI AI""', '');
            return [getComputedStyle(icon).fontFamily, getComputedStyle(icon, '::before').content, String(faces.length)];
        }");

        StringAssert.Contains(glyph[0], "Fabric MDL2 bit BlazorUI AI", $"The copy button's glyph is not drawn with the AI package's font ({glyph[0]}).");
        Assert.IsFalse(glyph[1] is "none" or "normal" or "" or "\"\"", $"The copy button's glyph is not drawn: {glyph[1]}");
        Assert.AreNotEqual("0", glyph[2], "The AI package's glyph font did not load.");

        await Page.Locator(".bit-aicpb button").First.ClickAsync();
        await WaitForAsync("() => typeof BitBlazorUI.AiCopyButton?.copy === 'function'", "A press of the copy button did not reach its script");
    }

    /// <summary>
    /// The AI package's chat (BitAiChat over the app's own transport): a prompt typed into its composer and sent with
    /// Enter, which the composer's script takes, is answered in its transcript.
    /// </summary>
    public async Task AssertAiChatAnswersAsync()
    {
        // From the keyboard: the app's nav panel, fixed to the side of the page, lies over the controls further down it.
        await Page.Locator(".bit-aicht textarea.bit-aipri-inp").First.FocusAsync();
        await Page.Keyboard.TypeAsync("hello");
        await Page.Keyboard.PressAsync("Enter");

        await WaitForAsync("() => document.querySelector('.bit-aicht .bit-aimsl')?.textContent.includes('You said: hello') === true", "BitAiChat did not answer the prompt sent with Enter");
    }

    public async Task WaitForAsync(string expression, string failure)
    {
        try
        {
            await Page.WaitForFunctionAsync(expression, null, new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            Assert.Fail($"{failure}. Errors so far: {string.Join(" | ", Errors)}");
        }
    }

    /// <summary>
    /// The classes on the page that a component stylesheet of the packages styles, where that stylesheet is not in the
    /// published bundle: the first segment of each bit- class (bit-btn) names the stylesheet whose classes start with it.
    /// </summary>
    public async Task AssertEveryComponentIsStyledAsync(PublishResult published, params string[] packages)
    {
        var classes = await Page.EvaluateAsync<string[]>("() => [...new Set([...document.querySelectorAll('[class]')].flatMap(e => [...e.classList]).filter(c => c.startsWith('bit-')))]");
        var prefixes = ChunkPrefixes(packages);
        var kept = packages.SelectMany(p => published.KeptStyles(p)).ToHashSet();

        // A class is styled when any stylesheet naming its classes with its prefix was kept: a family split over several
        // stylesheets shares one prefix, and the one kept need not be the first of them.
        var unstyled = classes.Select(c => ClassPrefix().Match(c))
                              .Where(m => m.Success && prefixes.ContainsKey(m.Groups["prefix"].Value))
                              .Select(m => (Class: m.Value, Chunks: prefixes[m.Groups["prefix"].Value]))
                              .Where(x => x.Chunks.Any(kept.Contains) is false)
                              .Select(x => $"{x.Class} ({string.Join(" or ", x.Chunks)})")
                              .Distinct()
                              .ToList();

        Assert.AreEqual(0, unstyled.Count, $"The page renders classes whose stylesheet the trimmed bundle left out: {string.Join(", ", unstyled)}");
    }

    // The class prefix each component stylesheet names its classes with (btn for BitButton): the one most of its class
    // selectors start with - and, for each prefix, every stylesheet it is the prefix of.
    private static Dictionary<string, List<string>> ChunkPrefixes(IEnumerable<string> packages)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var pieces in packages.Select(PublishResult.GetPieces))
        {
            foreach (var chunk in pieces.StyleChunks)
            {
                var prefix = ClassPrefix().Matches(File.ReadAllText(pieces.Resolve(chunk.File)))
                                          .GroupBy(m => m.Groups["prefix"].Value)
                                          .OrderByDescending(g => g.Count())
                                          .Select(g => g.Key)
                                          .FirstOrDefault();
                if (prefix is null) continue;

                if (result.TryGetValue(prefix, out var chunks) is false) result[prefix] = chunks = [];
                chunks.Add(chunk.Name);
            }
        }
        return result;
    }

    [GeneratedRegex(@"(?<![\w-])bit-(?<prefix>[a-z0-9]+)(?=[-\s.:,{)\]>]|$)")]
    private static partial Regex ClassPrefix();

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
