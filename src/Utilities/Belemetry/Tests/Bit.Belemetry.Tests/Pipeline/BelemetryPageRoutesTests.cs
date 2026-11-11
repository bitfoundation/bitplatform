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
        Assert.ContainsSingle(r => string.Equals(r, "/route-test/orders", StringComparison.OrdinalIgnoreCase), routes, "Two pages declaring one route list it once.");
        Assert.DoesNotContain("/route-test/not-a-component", routes, "Only a component is routed.");
        Assert.DoesNotContain("/route-test/internal", routes, "The router only looks at exported types.");
    }

    [TestMethod]
    public void Framework_IsTheAssembliesMicrosoftSigns_NotTheOnesNamedLikeThem()
    {
        Assert.IsTrue(BelemetryPageRoutes.IsFramework(typeof(object).Assembly.GetName()));
        Assert.IsTrue(BelemetryPageRoutes.IsFramework(typeof(ComponentBase).Assembly.GetName()));
        Assert.IsTrue(BelemetryPageRoutes.IsFramework(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollection).Assembly.GetName()));
        Assert.IsTrue(BelemetryPageRoutes.IsFramework(typeof(Uri).Assembly.GetName()));
        Assert.IsTrue(BelemetryPageRoutes.IsFramework(typeof(BelemetryPageRoutesTests).Assembly.GetReferencedAssemblies().First(r => r.Name == "System.Runtime")));

        Assert.IsFalse(BelemetryPageRoutes.IsFramework(new AssemblyName("Microsoft.Contoso.Pages")), "An app's library named like the framework's still routes its pages.");
        Assert.IsFalse(BelemetryPageRoutes.IsFramework(typeof(BelemetryPageRoutesTests).Assembly.GetName()));
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
        Assert.HasCount(routes.Length, routes.Distinct(StringComparer.OrdinalIgnoreCase));
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Routing discovery fixtures intentionally expose their public types.")]
    public sealed class RouteTestOrderPage : ComponentBase { }

    [Route("/Route-Test/Orders")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Routing discovery fixtures intentionally expose their public types.")]
    public sealed class RouteTestOrdersAgainPage : ComponentBase { }

    [Route("/route-test/users/{name}")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Routing discovery fixtures intentionally expose their public types.")]
    public sealed class RouteTestUserPage : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    [Route("/route-test/not-a-component")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Routing discovery fixtures intentionally expose their public types.")]
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
