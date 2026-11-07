using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// axe-core, the engine accessibility audits use, run over one element of the page under test: the WCAG 2.0, 2.1 and
/// 2.2 A and AA rules (<see cref="Tags"/>), through <see cref="E2ETestBase.AssertNoAxeViolationsAsync"/>.
/// </summary>
/// <remarks>
/// <para>The policy every scan follows:</para>
/// <list type="number">
/// <item>A scan covers one component, or one section of a test page (<c>#color</c>), never the whole page: the page's own
/// heading and status line are not the component's, and neither is the demo site's chrome.</item>
/// <item>A violation of the component's own is fixed, not suppressed, when the fix belongs to the task at hand.</item>
/// <item>One that cannot be fixed there is suppressed with an <see cref="AxeSuppression"/>: one rule, on the elements one
/// selector finds (the narrowest that covers it, never a whole section), with the reason, and the link of the issue that
/// tracks its fix (<see cref="AxeSuppression.KnownIssue"/>). A violation the test page renders on purpose, for another
/// test to check what the component does with it (a button with no name at all), is a
/// <see cref="AxeSuppression.Fixture"/>, with the reason and no issue.</item>
/// <item>A suppression that matches nothing fails the scan, so it goes once the violation does.</item>
/// <item>Passing the scan is necessary, not sufficient: axe sees the DOM at one moment, and the keyboard, the screen
/// readers and the states a test does not reach are checked by hand.</item>
/// </list>
/// <para>
/// Through <see cref="LocatorExtensions.RunAxe(ILocator, AxeRunOptions)"/>, which runs <c>axe.run</c> once over the
/// element; the page-wide <c>RunAxe</c> runs axe frame by frame and finishes the run in a page of its own, which only
/// cross-origin frames need. Axe's best practices are left out: their page-level rules (one <c>main</c> landmark, a
/// first-level heading, all content in landmarks) say nothing about a component.
/// </para>
/// </remarks>
internal static class E2EAxe
{
    /// <summary>
    /// The rules a scan runs: those of WCAG 2.0, 2.1 and 2.2 at levels A and AA.
    /// </summary>
    public static readonly string[] Tags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];

    private static readonly AxeRunOptions Options = new()
    {
        RunOnly = new RunOnlyOptions { Type = "tag", Values = [.. Tags] },
    };

    /// <summary>
    /// Scans the one element <paramref name="scope"/> finds once the page is at rest, and fails with every violation the
    /// suppressions do not cover, and every suppression that covered nothing.
    /// </summary>
    public static async Task AssertNoViolationsAsync(IPage page, string scope, IReadOnlyCollection<AxeSuppression> suppressions)
    {
        var root = page.Locator(scope);
        await Expect(root).ToHaveCountAsync(1);

        await WaitForRestAsync(page);

        AxeResult result = await root.RunAxe(Options);

        var unused = new HashSet<AxeSuppression>(suppressions);
        var report = new StringBuilder();
        var violations = 0;

        foreach (AxeResultItem violation in result.Violations)
        {
            var nodes = new List<AxeResultNode>();
            foreach (AxeResultNode node in violation.Nodes)
            {
                var suppression = await FindSuppressionAsync(page, violation.Id, node, suppressions);
                if (suppression is null)
                {
                    nodes.Add(node);
                }
                else
                {
                    unused.Remove(suppression);
                }
            }

            if (nodes.Count == 0) continue;

            violations += nodes.Count;
            report.Append($"\n- [{violation.Impact}] {violation.Id}: {violation.Help} ({violation.HelpUrl})");
            foreach (AxeResultNode node in nodes.Take(5))
            {
                var html = node.Html.Length > 200 ? node.Html[..200] + "..." : node.Html;
                var why = (node.Any ?? []).Concat(node.All ?? []).Concat(node.None ?? []).Select(check => check.Message).Distinct();
                report.Append($"\n    {node.Target}\n      {html}\n      {string.Join("; ", why)}");
            }

            if (nodes.Count > 5)
            {
                report.Append($"\n    and {nodes.Count - 5} more");
            }
        }

        foreach (var suppression in unused)
        {
            report.Append($"\n- The suppression of {suppression.Rule} on {suppression.Selector} matched nothing: remove it ({suppression.Reason}).");
        }

        if (report.Length > 0)
        {
            Assert.Fail($"axe-core {result.TestEngine?.Version} found {violations} WCAG 2.2 A/AA violation(s) under {scope}:{report}");
        }
    }

    /// <summary>
    /// The suppression of the rule that covers the node: the one whose selector finds the node's element or one of its
    /// ancestors, so a selector naming a component covers the parts of it axe reports. A node in a shadow root is found
    /// through its hosts, which count as its ancestors; one in an iframe is never covered and stays in the report, since
    /// the selectors are the page's.
    /// </summary>
    private static async Task<AxeSuppression?> FindSuppressionAsync(IPage page, string rule, AxeResultNode node, IReadOnlyCollection<AxeSuppression> suppressions)
    {
        // One list per frame, one selector per shadow root in each: AxeSelector.Selector throws for anything but one of one.
        var frames = node.Target.FrameShadowSelectors;
        if (frames.Count != 1) return null;

        string[] shadowPath = [.. frames[0]];

        foreach (var suppression in suppressions)
        {
            if (suppression.Rule != rule) continue;

            var covered = await page.EvaluateAsync<bool>(
                """
                ([shadowPath, selector]) => {
                  let el = document.querySelector(shadowPath[0]);
                  for (const part of shadowPath.slice(1)) el = el?.shadowRoot?.querySelector(part) ?? null;
                  for (let n = el; n; ) {
                    if (n.closest(selector)) return true;
                    const root = n.getRootNode();
                    n = root instanceof ShadowRoot ? root.host : null;
                  }
                  return false;
                }
                """,
                new object[] { shadowPath, suppression.Selector });

            if (covered) return suppression;
        }

        return null;
    }

    /// <summary>
    /// Waits until every animation and transition that ends has ended: axe reads colours as they are painted, and an
    /// element still fading in is painted blended with what is behind it. The ones that never end (a spinner) are left.
    /// </summary>
    private static Task WaitForRestAsync(IPage page)
    {
        return page.EvaluateAsync(
            """
            async () => {
              const ending = document.getAnimations().filter(a => a.playState === 'running' && a.effect?.getComputedTiming().endTime !== Infinity);
              await Promise.all(ending.map(a => a.finished.catch(() => null)));
            }
            """);
    }
}

/// <summary>
/// One violation a scan lets through: one axe rule, on the elements one CSS selector finds and on what is inside them,
/// and why. See <see cref="E2EAxe"/> for when one is allowed.
/// </summary>
public sealed record AxeSuppression
{
    /// <summary>
    /// Through <see cref="KnownIssue"/> or <see cref="Fixture"/>, which say which kind of suppression it is.
    /// </summary>
    private AxeSuppression(string rule, string selector, string reason, string? issue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Rule = rule;
        Selector = selector;
        Reason = reason;
        Issue = issue;
    }

    /// <summary>
    /// The id of the axe rule (<c>color-contrast</c>, <c>button-name</c>).
    /// </summary>
    public string Rule { get; }

    /// <summary>
    /// The CSS selector of the elements the rule may fail on.
    /// </summary>
    public string Selector { get; }

    /// <summary>
    /// Why the violation is let through.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// The issue that tracks the fix, or null for a <see cref="Fixture"/>.
    /// </summary>
    public string? Issue { get; }

    /// <summary>
    /// A defect of the component that is not fixed yet: the issue that tracks its fix is required.
    /// </summary>
    public static AxeSuppression KnownIssue(string rule, string selector, string reason, string issue)
    {
        if (Uri.TryCreate(issue, UriKind.Absolute, out var uri) is false || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException($"A known issue is linked by the https URL of its issue, not '{issue}'.", nameof(issue));
        }

        return new(rule, selector, reason, issue);
    }

    /// <summary>
    /// A violation the test page renders on purpose, for another test to check what the component does with it.
    /// </summary>
    public static AxeSuppression Fixture(string rule, string selector, string reason) => new(rule, selector, reason, null);
}
