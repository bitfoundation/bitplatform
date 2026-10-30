using Deque.AxeCore.Commons;
using Microsoft.Playwright;

namespace Bit.Bheadless.Tests.E2E.Infrastructure;

/// <summary>
/// axe-core - the copy Deque.AxeCore.Commons bundles - run inside the page under test over the demo content
/// (<c>main</c>; the site's chrome is out of scope), against the WCAG 2.0-2.2 A and AA rules and axe's best practices.
/// </summary>
/// <remarks>
/// Not through Deque.AxeCore.Playwright's <c>RunAxe</c>: that runs axe in parts, frame by frame, and finishes the run in a
/// page of its own, which is what cross-origin frames need and the demo has none of. A single <c>axe.run</c> asks nothing
/// of the engine but script.
/// </remarks>
public static class Axe
{
    private static readonly Lazy<string> Script = new(() => new BundledAxeScriptProvider().GetScript());

    private static readonly string[] Tags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa", "best-practice"];

    /// <summary>
    /// Every violation under <paramref name="include"/>, once the page is at rest: its impact, rule and help, then for up
    /// to five of its nodes the selector, the markup and what axe says is wrong.
    /// </summary>
    public static async Task<string[]> ViolationsAsync(IPage page, string include = "main")
    {
        await WaitForRestAsync(page);

        if (await page.EvaluateAsync<bool>("() => typeof window.axe === 'undefined'"))
        {
            await page.EvaluateAsync(Script.Value);
        }

        return await page.EvaluateAsync<string[]>(
            """
            async ([include, tags]) => {
              const result = await axe.run({ include: [include] }, { runOnly: { type: 'tag', values: tags } });
              return result.violations.map(v => `[${v.impact}] ${v.id}: ${v.help} (${v.helpUrl})\n` + v.nodes.slice(0, 5).map(n =>
                `    ${n.target.join(' ')}\n      ${n.html}\n      ${(n.failureSummary ?? '').replace(/\n/g, '\n      ')}`).join('\n'));
            }
            """, new object[] { include, Tags });
    }

    /// <summary>
    /// Waits until no transition runs: a <c>BitBhTransition</c> marks its element <c>[data-transition]</c> from its first
    /// render (prerendered too) until the phase ends, and a CSS animation or transition is finished once its
    /// <c>Animation.finished</c> settles - endless ones (spinners) aside. Audited mid-fade, text has the contrast of a
    /// frame: the transition page's appear sample starts fading in on the first interactive render, and measured 1.98:1.
    /// </summary>
    private static async Task WaitForRestAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => document.querySelector('[data-transition]') === null", null,
            new PageWaitForFunctionOptions { Timeout = DemoTest.ExpectTimeout });

        await page.EvaluateAsync(
            """
            async () => {
              const running = document.getAnimations().filter(a => a.playState === 'running' && a.effect?.getComputedTiming().endTime !== Infinity);
              await Promise.all(running.map(a => a.finished.catch(() => null)));
            }
            """);
    }
}
