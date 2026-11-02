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
            .Select(source => (Routes: Regex.Matches(source, "@page\\s+\"([^\"]+)\"").Select(route => route.Groups[1].Value).ToArray(),
                               Samples: Regex.Matches(source, "<CodeSnippet\\s").Count))
            // A component without @page is not a page of its own: whatever page renders it counts its samples.
            .Where(page => page.Samples > 0 && page.Routes.Length > 0)
            .Select(page => new object[] { PathOf(page.Routes.OrderByDescending(route => route.Contains('{')).First()), page.Samples });

    /// <summary>
    /// A route with each parameter filled in by a value its constraint accepts - a constrained parameter given a
    /// word would not match the route at all, and the page would answer 404.
    /// </summary>
    private static string PathOf(string route) =>
        Regex.Replace(route, "\\{\\*{0,2}[^}:?]+(?::([^}?]+))?\\??\\}", parameter => parameter.Groups[1].Value.ToLowerInvariant() switch
        {
            "int" or "long" or "decimal" or "double" or "float" => "1",
            "bool" => "true",
            "guid" => Guid.Empty.ToString(),
            "datetime" => "2026-01-01",
            _ => "e2e-snippets",
        });

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
