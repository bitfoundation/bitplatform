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
        Assert.AreEqual(0, BelemetryPageRoutes.Discover(typeof(object).Assembly).Length);
    }
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
