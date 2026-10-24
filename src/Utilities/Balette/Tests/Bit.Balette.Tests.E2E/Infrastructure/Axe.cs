using Deque.AxeCore.Commons;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E.Infrastructure;

/// <summary>
/// axe-core - the copy Deque.AxeCore.Commons bundles - run inside the page under test, on one element, against the WCAG
/// 2.0-2.2 A and AA rules.
/// </summary>
/// <remarks>
/// Not through Deque.AxeCore.Playwright's <c>RunAxe</c>: that runs axe in parts, frame by frame, and finishes the run in a
/// page of its own, which is what cross-origin frames need and the palette has none of. In WebKit that hand-over ran the
/// test process out of memory and closed the browser. A single <c>axe.run</c> asks nothing of the engine but script.
/// </remarks>
public static class Axe
{
    private static readonly Lazy<string> Script = new(() => new BundledAxeScriptProvider().GetScript());

    private static readonly string[] WcagTags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];

    /// <summary>Every violation in <paramref name="element"/>: its rule, impact and help, and the markup of up to three of its nodes.</summary>
    public static async Task<string[]> ViolationsAsync(ILocator element)
    {
        if (await element.Page.EvaluateAsync<bool>("() => typeof window.axe === 'undefined'"))
        {
            await element.Page.EvaluateAsync(Script.Value);
        }

        return await element.EvaluateAsync<string[]>(
            """
            async (element, tags) => {
              const result = await axe.run(element, { runOnly: { type: 'tag', values: tags } });
              return result.violations.map(v => `${v.id} (${v.impact}): ${v.help}\n    ` + v.nodes.slice(0, 3).map(n => n.html).join('\n    '));
            }
            """, WcagTags);
    }
}
