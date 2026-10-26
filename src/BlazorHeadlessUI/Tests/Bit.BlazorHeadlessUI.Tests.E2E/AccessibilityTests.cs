using Bit.BlazorHeadlessUI.Demo.Client.Core.Shared;
using Bit.BlazorHeadlessUI.Tests.E2E.Infrastructure;

namespace Bit.BlazorHeadlessUI.Tests.E2E;

/// <summary>
/// Every demo page, in both styling sections, passes axe-core (WCAG 2.2 A and AA plus best practices, over <c>main</c>)
/// in its initial, interactive state, and renders without a script error. The pages come from the demo's own catalogue
/// (<see cref="DemoCatalog"/>), so a page added to the site is audited without touching this suite.
/// </summary>
/// <remarks>An automated audit finds a part of the problems; it is not a claim of WCAG conformance.</remarks>
[TestClass]
public sealed class AccessibilityTests : DemoTest
{
    public static IEnumerable<object[]> Slugs => DemoCatalog.Components.Select(c => new object[] { c.Slug });

    [TestMethod]
    [DynamicData(nameof(Slugs))]
    public Task Css_page_is_accessible(string slug) => AuditAsync(DemoSection.Css, slug);

    [TestMethod]
    [DynamicData(nameof(Slugs))]
    public Task Tailwind_page_is_accessible(string slug) => AuditAsync(DemoSection.Tailwind, slug);

    private async Task AuditAsync(DemoSection section, string slug)
    {
        var page = await OpenDemoAsync("/" + DemoCatalog.Href(section, slug).TrimStart('/'));

        var violations = await Axe.ViolationsAsync(page);
        var errors = TakePageErrors();

        // Both in one message: a page can break both ways at once, and the second would otherwise go unreported.
        if (violations.Length > 0 || errors.Length > 0)
            Assert.Fail($"{page.Url}:{Environment.NewLine}" +
                        $"axe-core found {violations.Length} violation(s){(violations.Length > 0 ? ":" : ".")}{Environment.NewLine}" +
                        string.Join(Environment.NewLine + Environment.NewLine, violations) + Environment.NewLine +
                        $"The page reported {errors.Length} error(s){(errors.Length > 0 ? ":" : ".")}{Environment.NewLine}" +
                        string.Join(Environment.NewLine, errors));
    }
}
