using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryPageRoutesTests
{
    [TestMethod]
    public void Discover_FindsTheRoutesOfTheRoutableComponents_OnceEach()
    {
        var routes = BelemetryPageRoutes.Discover(typeof(BelemetryPageRoutesTests).Assembly);

        CollectionAssert.IsSubsetOf(new[] { "/route-test/orders/{id:int}", "/route-test/orders", "/route-test/users/{name}" }, routes);
        Assert.AreEqual(1, routes.Count(r => string.Equals(r, "/route-test/orders", StringComparison.OrdinalIgnoreCase)), "Two pages declaring one route list it once.");
        CollectionAssert.DoesNotContain(routes, "/route-test/not-a-component", "Only a component is routed.");
        CollectionAssert.DoesNotContain(routes, "/route-test/internal", "The router only looks at exported types.");
    }

    [TestMethod]
    public void Discover_FindsNothing_InAnAssemblyWithoutComponents()
    {
        Assert.IsEmpty(BelemetryPageRoutes.Discover(typeof(object).Assembly));
    }

    [TestMethod]
    public void Discover_KeepsTheRoutesOfAnAssembly_WhoseReferencesCannotBeRead()
    {
        // NativeAOT keeps no metadata of an assembly's references: its own pages still count.
        var routes = BelemetryPageRoutes.Discover(new NoReferencesAssembly(typeof(BelemetryPageRoutesTests).Assembly));

        CollectionAssert.IsSubsetOf(new[] { "/route-test/orders/{id:int}", "/route-test/orders", "/route-test/users/{name}" }, routes);
    }

    [TestMethod]
    public void Read_ListsTheRoutesOfExactlyTheGivenAssemblies()
    {
        var routes = BelemetryPageRoutes.Read([typeof(BelemetryPageRoutesTests).Assembly, typeof(BelemetryPageRoutesTests).Assembly, typeof(object).Assembly]);

        CollectionAssert.IsSubsetOf(new[] { "/route-test/orders/{id:int}", "/route-test/orders", "/route-test/users/{name}" }, routes);
        Assert.AreEqual(routes.Length, routes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [TestMethod]
    public void Settings_ReadThePageRouteAssemblies_InsteadOfDiscovering()
    {
        var settings = TestHost.CreateSettings(o =>
        {
            o.Browser.DiscoverPageRoutes = true;
            o.Browser.PageRoutes.Add("/configured");
            o.Browser.PageRouteAssemblies.Add(typeof(BelemetryPageRoutesTests).Assembly);
        });

        Assert.AreEqual("/configured", settings.PageRoutes[0]);
        CollectionAssert.IsSubsetOf(new[] { "/route-test/orders/{id:int}", "/route-test/orders" }, settings.PageRoutes.ToArray());
        Assert.IsFalse(settings.TakeNoPageRoutesWarning(), "Routes were found.");
    }

    [TestMethod]
    public void Settings_WarnOnce_WhenDiscoveryFindsNoRoute()
    {
        var settings = TestHost.CreateSettings(o =>
        {
            o.Browser.DiscoverPageRoutes = true;
            o.Browser.PageRouteAssemblies.Add(typeof(object).Assembly);
        });

        Assert.IsEmpty(settings.PageRoutes);
        Assert.IsTrue(settings.TakeNoPageRoutesWarning());
        Assert.IsFalse(settings.TakeNoPageRoutesWarning(), "Once.");
    }

    [Route("/route-test/orders/{id:int}")]
    [Route("/route-test/orders")]
    public sealed class RouteTestOrderPage : ComponentBase { }

    [Route("/Route-Test/Orders")]
    public sealed class RouteTestOrdersAgainPage : ComponentBase { }

    [Route("/route-test/users/{name}")]
    public sealed class RouteTestUserPage : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    [Route("/route-test/not-a-component")]
    public sealed class RouteTestNotAComponent { }

    [Route("/route-test/internal")]
    internal sealed class RouteTestInternalPage : ComponentBase { }

    /// <summary>An assembly as NativeAOT shows it: its types, but not its references.</summary>
    private sealed class NoReferencesAssembly(Assembly inner) : Assembly
    {
        public override AssemblyName GetName() => inner.GetName();

        public override AssemblyName[] GetReferencedAssemblies() => throw new PlatformNotSupportedException();

        public override Type[] GetExportedTypes() => inner.GetExportedTypes();
    }
}
