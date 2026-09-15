using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.SecurityHeaders;

/// <summary>
/// The app-wide Content-Security-Policy (Client.Core's ContentSecurityPolicy.razor) in a real browser: every origin the
/// deployed app loads from must be in it, or the browser blocks it - silently, from the user's point of view.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebContentSecurityPolicyTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (prerendered)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (prerendered)")]
    [DataRow(App.Sales, DisplayName = "Sales (prerendered)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    [DataRow(App.TodoAot, DisplayName = "TodoAot (Static Web App)")]
    public async Task App_Should_BootWithoutAnyCspViolation(App app)
    {
        // Before any page script, so a violation during boot is caught too.
        await Page.AddInitScriptAsync("""
            window.__cspViolations = [];
            document.addEventListener('securitypolicyviolation', e => window.__cspViolations.push(`${e.effectiveDirective} blocked ${e.blockedURI} (${e.sourceFile}:${e.lineNumber})`));
            """);

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        // The standalone apps have no server to prerender it: AppHeadCoordinator adds it through HeadOutlet once Blazor starts.
        await Expect(page.Locator("head meta[http-equiv='Content-Security-Policy']")).ToHaveCountAsync(1);

        // Lazy loaded parts (fonts, images, the SignalR connection) come after the first render.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = (float)TimeSpan.FromMinutes(1).TotalMilliseconds });

        var violations = await page.EvaluateAsync<string[]>("window.__cspViolations");
        Assert.IsEmpty(violations, $"{app}: {string.Join(Environment.NewLine, violations)}");
    }
}
