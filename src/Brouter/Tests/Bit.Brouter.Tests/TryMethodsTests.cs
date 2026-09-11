using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class TryMethodsTests : BunitTestContext
{
    private (IRenderedComponent<NamedRouteHost> Cut, IBrouter Brouter, BunitNavigationManager Nav) RenderMounted()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/users/1");

        var cut = RenderComponent<NamedRouteHost>(p => p
            .Add(h => h.Name, "user")
            .Add(h => h.Path, "/users/{id:int}"));

        return (cut, Services.GetRequiredService<IBrouter>(), nav);
    }

    [TestMethod]
    public async Task Try_methods_are_noops_when_no_brouter_is_mounted()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/start");
        var brouter = Services.GetRequiredService<IBrouter>();

        Assert.IsFalse(brouter.IsMounted);

        Assert.IsFalse(brouter.TryNavigate("/elsewhere"));
        Assert.IsNull(await brouter.TryNavigateAsync("/elsewhere"));
        Assert.IsFalse(brouter.TryBack());
        Assert.IsFalse(await brouter.TryBackAsync(2));
        Assert.IsFalse(brouter.TryForward());
        Assert.IsFalse(await brouter.TryForwardAsync(2));
        Assert.IsFalse(brouter.TryNavigateToName("user", new Dictionary<string, object?> { ["id"] = 1 }));
        Assert.IsFalse(await brouter.TryRevalidateAsync());
        Assert.IsFalse(await brouter.TryReloadAsync());
        Assert.IsFalse(await brouter.TryPreloadAsync("/elsewhere"));
        Assert.IsFalse(brouter.TryClearKeepAlive());
        Assert.IsFalse(brouter.TryClearKeepAlive(includeActive: true));

        Assert.IsFalse(brouter.TryResolveUrl("user", out var url, new Dictionary<string, object?> { ["id"] = 1 }));
        Assert.IsNull(url);

        var mutated = false;
        Assert.IsFalse(brouter.TryNavigateWithQuery(q => { mutated = true; q.Set("page", 2); }));
        Assert.IsFalse(mutated, "The query mutation must not run when nothing will navigate.");

        Assert.AreEqual("http://localhost/start", nav.Uri);
    }

    [TestMethod]
    public void The_throwing_members_still_throw_when_no_brouter_is_mounted()
    {
        var brouter = Services.GetRequiredService<IBrouter>();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => brouter.Navigate("/elsewhere"));
        StringAssert.Contains(ex.Message, nameof(IBrouter.IsMounted));

        Assert.ThrowsExactly<InvalidOperationException>(() => brouter.Back());
        Assert.ThrowsExactly<InvalidOperationException>(() => brouter.ResolveUrl("user"));
        Assert.ThrowsExactly<InvalidOperationException>(() => brouter.ClearKeepAlive());
    }

    [TestMethod]
    public async Task Try_methods_do_the_work_when_a_brouter_is_mounted()
    {
        var (cut, brouter, nav) = RenderMounted();

        Assert.IsTrue(brouter.IsMounted);

        Assert.IsTrue(brouter.TryResolveUrl("user", out var url, new Dictionary<string, object?> { ["id"] = 7 }));
        Assert.AreEqual("/users/7", url);

        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryNavigate("/users/2")));
        cut.WaitForAssertion(() => Assert.AreEqual("/users/2", brouter.Location.Path));

        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryNavigateToName("user", new Dictionary<string, object?> { ["id"] = 3 })));
        cut.WaitForAssertion(() => Assert.AreEqual("/users/3", brouter.Location.Path));

        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryNavigateWithQuery(q => q.Set("page", 2))));
        cut.WaitForAssertion(() => Assert.AreEqual("http://localhost/users/3?page=2", nav.Uri));

        var outcome = await cut.InvokeAsync(() => brouter.TryNavigateAsync("/users/4").AsTask());
        Assert.IsNotNull(outcome);
        Assert.AreEqual(BrouterNavigationStatus.Succeeded, outcome.Value.Status);

        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryRevalidateAsync().AsTask()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryReloadAsync().AsTask()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryPreloadAsync("/users/5").AsTask()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryBackAsync().AsTask()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryForwardAsync().AsTask()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryBack()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryForward()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryClearKeepAlive()));
        Assert.IsTrue(await cut.InvokeAsync(() => brouter.TryClearKeepAlive(includeActive: true)));
    }

    [TestMethod]
    public void Try_methods_still_reject_invalid_arguments_when_a_brouter_is_mounted()
    {
        // Only the mount state is tolerated; a bad call is still a bug worth surfacing.
        var (_, brouter, _) = RenderMounted();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => brouter.TryBackAsync(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => brouter.TryForwardAsync(0));
        Assert.ThrowsExactly<InvalidOperationException>(() => brouter.TryResolveUrl("no-such-route", out _));
        Assert.ThrowsExactly<ArgumentException>(() => brouter.TryResolveUrl("user", out _));
    }

    [TestMethod]
    public async Task Try_methods_become_noops_again_once_the_brouter_is_disposed()
    {
        var (_, brouter, nav) = RenderMounted();
        Assert.IsTrue(brouter.IsMounted);

        await Context!.DisposeComponentsAsync();

        Assert.IsFalse(brouter.IsMounted);
        Assert.IsFalse(brouter.TryNavigate("/users/9"));
        Assert.IsFalse(await brouter.TryRevalidateAsync());
        Assert.IsFalse(brouter.TryResolveUrl("user", out _, new Dictionary<string, object?> { ["id"] = 9 }));
        Assert.AreEqual("http://localhost/users/1", nav.Uri);
    }

    [TestMethod]
    public async Task Custom_implementations_get_working_Try_members_from_the_interface_defaults()
    {
        IBrouter mounted = new RecordingBrouter();
        IBrouter unmounted = new UnmountedRecordingBrouter();

        // A custom implementation that doesn't model mounting is assumed to always serve calls.
        Assert.IsTrue(mounted.IsMounted);
        Assert.IsTrue(mounted.TryNavigate("/a"));
        Assert.IsTrue(mounted.TryBack());
        Assert.IsTrue(mounted.TryResolveUrl("user", out var url));
        Assert.AreEqual("/resolved/user", url);
        CollectionAssert.AreEqual(new[] { "Navigate:/a", "Back", "ResolveUrl:user" }, ((RecordingBrouter)mounted).Calls);

        // One that does model it gets the no-op behavior without implementing a single Try member.
        Assert.IsFalse(unmounted.TryNavigate("/a"));
        Assert.IsFalse(unmounted.TryBack());
        Assert.IsFalse(unmounted.TryResolveUrl("user", out _));
        Assert.IsNull(await unmounted.TryNavigateAsync("/a"));
        Assert.IsFalse(await unmounted.TryRevalidateAsync());
        Assert.AreEqual(0, ((RecordingBrouter)unmounted).Calls.Count);
    }

    // Deliberately leaves IsMounted and every Try... member to the interface defaults.
    private class RecordingBrouter : IBrouter
    {
        public List<string> Calls { get; } = [];
        public BrouterLocation Location => BrouterLocation.Empty;
        public void Navigate(string url, bool replace = false, bool forceLoad = false, string? historyState = null) => Calls.Add($"Navigate:{url}");
        public void Back() => Calls.Add("Back");
        public void NavigateToName(string name, IReadOnlyDictionary<string, object?>? parameters = null,
                                   string? query = null, bool replace = false, string? historyState = null) => Calls.Add($"NavigateToName:{name}");
        public string ResolveUrl(string name, IReadOnlyDictionary<string, object?>? parameters = null, string? query = null)
        {
            Calls.Add($"ResolveUrl:{name}");
            return $"/resolved/{name}";
        }
        public event Func<BrouterNavigationContext, ValueTask>? OnNavigating { add { } remove { } }
        public event Func<BrouterNavigationContext, ValueTask>? OnNavigated { add { } remove { } }
        public event Func<BrouterNavigationContext, Exception?, ValueTask>? OnError { add { } remove { } }
    }

    private sealed class UnmountedRecordingBrouter : RecordingBrouter, IBrouter
    {
        bool IBrouter.IsMounted => false;
    }
}
