using System.Text.RegularExpressions;
using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The code under each sample: every <c>CodeSnippet</c> a page carries opens to its sample's source - found among
/// the sources the client embeds, highlighted, and with the copy button every code block gets.
/// </summary>
[TestClass]
public sealed class CodeSnippetTests : BcrdtTest
{
    /// <summary>
    /// Every page that shows the code of its samples, with how many it shows - counted from the <c>CodeSnippet</c>
    /// tags of its source, so a sample added to a page is checked without anyone having to list it here. A page
    /// with a route parameter (the live page's room) is opened at its parameterized route.
    /// </summary>
    public static IEnumerable<object[]> PagesWithSamples =>
        Directory.EnumerateFiles(RepoLayout.DemoPagesDirectory(), "*.razor", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Select(source => (Route: Regex.Matches(source, "@page\\s+\"([^\"]+)\"").Last().Groups[1].Value,
                               Samples: Regex.Matches(source, "<CodeSnippet\\s").Count))
            .Where(page => page.Samples > 0)
            .Select(page => new object[] { Regex.Replace(page.Route, "\\{[^}]+\\}", "e2e-snippets"), page.Samples });

    [TestMethod]
    [DynamicData(nameof(PagesWithSamples))]
    public async Task Every_sample_on_a_page_opens_to_its_highlighted_code(string path, int samples)
    {
        var page = await NewPageAsync(await NewContextAsync());
        var problems = TrackProblems(page, BaseUrl);
        await GotoAsync(page, path);
        await WaitForInteractiveAsync(page);

        // Some samples sit in sections that appear once the replicas are open.
        var snippets = page.Locator(".demo-snippet");
        await Assertions.Expect(snippets).ToHaveCountAsync(samples, new() { Timeout = 60_000 });

        for (int index = 0; index < samples; index++)
        {
            var snippet = snippets.Nth(index);
            var toggle = snippet.Locator("button").First;
            await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
            await toggle.ClickAsync();
            await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

            var code = snippet.Locator("pre.bb-code");
            await Assertions.Expect(code).ToBeVisibleAsync();
            await Assertions.Expect(code).Not.ToContainTextAsync("No sample named");
            await Assertions.Expect(code.Locator(".tok-key").First).ToBeVisibleAsync();
            await Assertions.Expect(code.Locator(".bb-copy-btn")).ToHaveCountAsync(1);
        }

        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, $"{path} reported:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }
}
