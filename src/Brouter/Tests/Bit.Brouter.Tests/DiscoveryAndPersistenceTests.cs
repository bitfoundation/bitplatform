using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class DiscoveryAndPersistenceTests : BunitTestContext
{
    [TestMethod]
    public void Attribute_routed_page_is_discovered_via_AppAssembly()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/42");

        var cut = RenderComponent<DiscoveryHost>();

        // The @page "/discovered/{id:int}" component was found by scanning the assembly - it was never
        // hand-declared in DiscoveryHost - and matched the URL.
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=discovered]")));
    }

    [TestMethod]
    public void Discovered_route_binds_route_and_query_parameters_conventionally()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/42?q=hello");

        var cut = RenderComponent<DiscoveryHost>();

        // Id bound from the {id:int} route parameter, Q bound from the ?q= query - both by name, with
        // no [BrouterParameter] annotations, exactly like a plain Blazor @page.
        cut.WaitForAssertion(() =>
            Assert.AreEqual("id:42 q:hello", cut.Find("[data-testid=discovered]").TextContent));
    }

    [TestMethod]
    public void Hand_declared_routes_still_match_alongside_discovered_ones()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<DiscoveryHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("home", cut.Find("[data-testid=home]").TextContent));
    }

    [TestMethod]
    public void Prerender_state_round_trips_loader_value_with_its_concrete_type()
    {
        var dto = new SampleDto { Name = "saleh", Count = 3 };

        var captured = BroutePrerenderState.Capture(dto);
        var restored = BroutePrerenderState.TryRestore(captured, out var value);

        Assert.IsTrue(restored);
        var typed = value as SampleDto;
        Assert.IsNotNull(typed);
        Assert.AreEqual("saleh", typed!.Name);
        Assert.AreEqual(3, typed.Count);
    }

    [TestMethod]
    public void Prerender_state_round_trips_a_null_loader_result()
    {
        var captured = BroutePrerenderState.Capture(null);

        // A persisted null is a real decision (loader ran and returned null): restoration must succeed
        // with a null value so the interactive pass skips re-running the loader.
        Assert.IsTrue(BroutePrerenderState.TryRestore(captured, out var value));
        Assert.IsNull(value);
    }

    [TestMethod]
    public void Prerender_state_round_trips_with_a_source_generated_resolver()
    {
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = PersistenceTestJsonContext.Default,
        };

        var dto = new SampleDto { Name = "aot", Count = 7 };
        var captured = BroutePrerenderState.Capture(dto, options);

        Assert.IsNotNull(captured);
        Assert.IsTrue(BroutePrerenderState.TryRestore(captured, out var value, options));
        var typed = value as SampleDto;
        Assert.IsNotNull(typed);
        Assert.AreEqual("aot", typed!.Name);
        Assert.AreEqual(7, typed.Count);
    }

    [TestMethod]
    public void Capture_of_a_type_the_resolver_does_not_cover_returns_null_instead_of_throwing()
    {
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = PersistenceTestJsonContext.Default,
        };

        // Uri isn't registered on the context: capture must degrade to "don't persist" (null),
        // never break prerender.
        var captured = BroutePrerenderState.Capture(new UncoveredDto(), options);

        Assert.IsNull(captured);
    }

    public sealed class UncoveredDto
    {
        public int X { get; set; }
    }

    [TestMethod]
    public void Prerender_key_is_stable_for_the_same_url_and_chain_position()
    {
        var a = BroutePrerenderState.MakeKey("/users/42", "?tab=1", 2);
        var b = BroutePrerenderState.MakeKey("/users/42", "?tab=1", 2);
        var different = BroutePrerenderState.MakeKey("/users/42", "?tab=1", 3);

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, different);
    }
}
