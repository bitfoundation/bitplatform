using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class NavigateAsyncTests : BunitTestContext
{
    private (IRenderedComponent<NavigateAsyncHost> Cut, IBrouter Brouter) RenderAtA()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");
        var cut = RenderComponent<NavigateAsyncHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));
        return (cut, Services.GetRequiredService<IBrouter>());
    }

    private BrouterNavigationOutcome Await(IRenderedComponent<NavigateAsyncHost> cut, ValueTask<BrouterNavigationOutcome> navigation)
    {
        var task = navigation.AsTask();
        cut.WaitForAssertion(() => Assert.IsTrue(task.IsCompleted, "navigation outcome did not resolve"));
        return task.GetAwaiter().GetResult();
    }

    [TestMethod]
    public async Task Successful_navigation_resolves_Succeeded()
    {
        var (cut, brouter) = RenderAtA();

        ValueTask<BrouterNavigationOutcome> navigation = default;
        await cut.InvokeAsync(() => { navigation = brouter.NavigateAsync("/b"); });
        var outcome = Await(cut, navigation);

        Assert.AreEqual(BrouterNavigationStatus.Succeeded, outcome.Status);
        Assert.IsTrue(outcome.Succeeded);
        Assert.IsNotNull(cut.Find("[data-testid=b]"));
    }

    [TestMethod]
    public async Task Guard_cancel_resolves_Cancelled_and_url_stays()
    {
        var (cut, brouter) = RenderAtA();
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        ValueTask<BrouterNavigationOutcome> navigation = default;
        await cut.InvokeAsync(() => { navigation = brouter.NavigateAsync("/blocked"); });
        var outcome = Await(cut, navigation);

        Assert.AreEqual(BrouterNavigationStatus.Cancelled, outcome.Status);
        Assert.IsTrue(nav.Uri.EndsWith("/a", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Guard_redirect_resolves_Redirected_with_the_target()
    {
        var (cut, brouter) = RenderAtA();

        ValueTask<BrouterNavigationOutcome> navigation = default;
        await cut.InvokeAsync(() => { navigation = brouter.NavigateAsync("/redirect"); });
        var outcome = Await(cut, navigation);

        Assert.AreEqual(BrouterNavigationStatus.Redirected, outcome.Status);
        Assert.AreEqual("/b", outcome.RedirectedTo);
        cut.WaitForAssertion(() => cut.Find("[data-testid=b]"));
    }

    [TestMethod]
    public async Task RedirectTo_route_resolves_Redirected()
    {
        var (cut, brouter) = RenderAtA();

        ValueTask<BrouterNavigationOutcome> navigation = default;
        await cut.InvokeAsync(() => { navigation = brouter.NavigateAsync("/route-redirect"); });
        var outcome = Await(cut, navigation);

        Assert.AreEqual(BrouterNavigationStatus.Redirected, outcome.Status);
        Assert.AreEqual("/b", outcome.RedirectedTo);
        cut.WaitForAssertion(() => cut.Find("[data-testid=b]"));
    }

    [TestMethod]
    public async Task Failing_loader_resolves_Failed_with_the_exception()
    {
        var (cut, brouter) = RenderAtA();

        ValueTask<BrouterNavigationOutcome> navigation = default;
        await cut.InvokeAsync(() => { navigation = brouter.NavigateAsync("/fail"); });
        var outcome = Await(cut, navigation);

        Assert.AreEqual(BrouterNavigationStatus.Failed, outcome.Status);
        Assert.IsInstanceOfType<InvalidOperationException>(outcome.Exception);
        Assert.IsTrue(outcome.Exception!.Message.Contains("loader exploded"));
    }

    [TestMethod]
    public async Task Unmatched_url_resolves_NotFound()
    {
        var (cut, brouter) = RenderAtA();

        ValueTask<BrouterNavigationOutcome> navigation = default;
        await cut.InvokeAsync(() => { navigation = brouter.NavigateAsync("/nope/nothing/here"); });
        var outcome = Await(cut, navigation);

        Assert.AreEqual(BrouterNavigationStatus.NotFound, outcome.Status);
    }

    [TestMethod]
    public async Task A_newer_navigation_supersedes_an_in_flight_awaiter()
    {
        var (cut, brouter) = RenderAtA();

        // First navigation parks inside the /slow loader, genuinely in-flight...
        ValueTask<BrouterNavigationOutcome> first = default;
        await cut.InvokeAsync(() => { first = brouter.NavigateAsync("/slow"); });

        // ...then a second navigation starts before the first's loader finishes.
        ValueTask<BrouterNavigationOutcome> second = default;
        await cut.InvokeAsync(() => { second = brouter.NavigateAsync("/b"); });

        var firstOutcome = Await(cut, first);
        var secondOutcome = Await(cut, second);
        cut.Instance.SlowGate.TrySetResult(); // release the parked loader so nothing leaks

        Assert.AreEqual(BrouterNavigationStatus.Superseded, firstOutcome.Status);
        Assert.AreEqual(BrouterNavigationStatus.Succeeded, secondOutcome.Status);
        cut.WaitForAssertion(() => cut.Find("[data-testid=b]"));
    }
}
