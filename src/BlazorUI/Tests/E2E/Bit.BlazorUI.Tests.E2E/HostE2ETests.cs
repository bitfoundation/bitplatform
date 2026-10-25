namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// Pins the host itself: it answers, every page it lists becomes interactive, and a page's errors reach the test.
/// </summary>
[TestClass]
public class HostE2ETests : E2ETestBase
{
    [TestMethod]
    public async Task TheHomePageBecomesInteractive()
    {
        await OpenAsync("/");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Bit.BlazorUI E2E Test Host");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task EveryListedPageBecomesInteractiveWithoutAnError()
    {
        await OpenAsync("/");

        var routes = await Page.Locator("#pages a").EvaluateAllAsync<string[]>("links => links.map(a => a.getAttribute('href'))");

        Assert.IsTrue(routes.Length > 0, "The host lists no E2E page.");

        var failures = new List<string>();

        foreach (var route in routes)
        {
            var before = ConsoleErrors.Count + PageErrors.Count;

            try
            {
                await OpenAsync(route);
            }
            catch (Exception ex)
            {
                failures.Add($"{route}: never became ready ({ex.Message.Split(Environment.NewLine)[0]})");
                continue;
            }

            if (await IsCircuitDeadAsync()) failures.Add($"{route}: the circuit died");
            if (ConsoleErrors.Count + PageErrors.Count > before) failures.Add($"{route}: raised an error");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }
}
