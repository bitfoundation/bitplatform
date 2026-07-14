using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Covers the unified by-name component parameter binding on hand-declared routes:
/// plain [Parameter] properties bind route values by name (Blazor-style, no opt-in flag),
/// [BrouterParameter(Name = ...)] remaps, unrelated [Parameter] properties stay untouched,
/// and invalid [BrouterParameter] annotations fail fast with actionable messages.
/// </summary>
[TestClass]
public class ParameterBindingTests : BunitTestContext
{
    [TestMethod]
    public void Plain_parameter_binds_route_value_by_name_on_hand_declared_route()
    {
        var (cut, _) = RenderAt<BindHost>("http://localhost/bind/7/anything");

        cut.WaitForAssertion(() => Assert.AreEqual("7", cut.Find("[data-testid=bind-id]").TextContent));
    }

    [TestMethod]
    public void BrouterParameter_name_override_remaps_route_value_to_differently_named_property()
    {
        var (cut, _) = RenderAt<BindHost>("http://localhost/bind/1/saleh");

        cut.WaitForAssertion(() => Assert.AreEqual("saleh", cut.Find("[data-testid=bind-display]").TextContent));
    }

    [TestMethod]
    public void Parameter_not_in_template_is_left_untouched()
    {
        var (cut, _) = RenderAt<BindHost>("http://localhost/bind/1/x");

        cut.WaitForAssertion(() => Assert.AreEqual("(null)", cut.Find("[data-testid=bind-other]").TextContent));
    }

    [TestMethod]
    public void Route_value_re_binds_when_navigation_changes_the_parameter()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/bind/1/first");

        var cut = RenderComponent<BindHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("first", cut.Find("[data-testid=bind-display]").TextContent));

        nav.NavigateTo("http://localhost/bind/2/second");
        cut.WaitForAssertion(() => Assert.AreEqual("2", cut.Find("[data-testid=bind-id]").TextContent));
        cut.WaitForAssertion(() => Assert.AreEqual("second", cut.Find("[data-testid=bind-display]").TextContent));
    }

    // ---- fail-fast diagnostics (BuildBindings runs on first GetBindings for a type) ---------

    private sealed class MissingParameterAttribute : ComponentBase
    {
        [BrouterParameter] public int Id { get; set; }
    }

    private sealed class MissingPublicSetter : ComponentBase
    {
        // BL0001 flags this at compile time for real components; disabled here on purpose so the
        // runtime fail-fast in BuildBindings (which also guards non-analyzed scenarios) is testable.
#pragma warning disable BL0001
        [Parameter, BrouterParameter] public int Id { get; private set; }
#pragma warning restore BL0001
    }

    private sealed class RouteAndQueryAmbiguous : ComponentBase
    {
        [Parameter, BrouterParameter, SupplyParameterFromQuery] public string? Value { get; set; }
    }

    private sealed class BrouterQueryMissingParameterAttribute : ComponentBase
    {
        [BrouterQuery] public string? Value { get; set; }
    }

    private sealed class BrouterRouteAndBrouterQueryAmbiguous : ComponentBase
    {
        [Parameter, BrouterParameter, BrouterQuery] public string? Value { get; set; }
    }

    private sealed class BothQueryAttributesAmbiguous : ComponentBase
    {
        [Parameter, BrouterQuery, SupplyParameterFromQuery] public string? Value { get; set; }
    }

    [TestMethod]
    public void BrouterParameter_without_Parameter_attribute_throws_with_actionable_message()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => BrouterTypedParameterCache.GetBindings(typeof(MissingParameterAttribute)));

        StringAssert.Contains(ex.Message, nameof(MissingParameterAttribute));
        StringAssert.Contains(ex.Message, "[Parameter]");
    }

    [TestMethod]
    public void BrouterParameter_without_public_setter_throws_with_actionable_message()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => BrouterTypedParameterCache.GetBindings(typeof(MissingPublicSetter)));

        StringAssert.Contains(ex.Message, nameof(MissingPublicSetter));
        StringAssert.Contains(ex.Message, "public setter");
    }

    [TestMethod]
    public void BrouterParameter_combined_with_SupplyParameterFromQuery_throws()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => BrouterTypedParameterCache.GetBindings(typeof(RouteAndQueryAmbiguous)));

        StringAssert.Contains(ex.Message, nameof(BrouterParameterAttribute));
        StringAssert.Contains(ex.Message, nameof(SupplyParameterFromQueryAttribute));
    }

    [TestMethod]
    public void BrouterQuery_without_Parameter_attribute_throws_with_actionable_message()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => BrouterTypedParameterCache.GetBindings(typeof(BrouterQueryMissingParameterAttribute)));

        StringAssert.Contains(ex.Message, nameof(BrouterQueryMissingParameterAttribute));
        StringAssert.Contains(ex.Message, "[Parameter]");
    }

    [TestMethod]
    public void BrouterParameter_combined_with_BrouterQuery_throws()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => BrouterTypedParameterCache.GetBindings(typeof(BrouterRouteAndBrouterQueryAmbiguous)));

        StringAssert.Contains(ex.Message, nameof(BrouterParameterAttribute));
        StringAssert.Contains(ex.Message, nameof(BrouterQueryAttribute));
    }

    [TestMethod]
    public void BrouterQuery_combined_with_SupplyParameterFromQuery_throws()
    {
        // Combining them is self-defeating: the framework supplier reacts to its own attribute
        // regardless of Brouter, re-introducing the type restrictions [BrouterQuery] escapes.
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => BrouterTypedParameterCache.GetBindings(typeof(BothQueryAttributesAmbiguous)));

        StringAssert.Contains(ex.Message, nameof(BrouterQueryAttribute));
        StringAssert.Contains(ex.Message, nameof(SupplyParameterFromQueryAttribute));
    }
}
