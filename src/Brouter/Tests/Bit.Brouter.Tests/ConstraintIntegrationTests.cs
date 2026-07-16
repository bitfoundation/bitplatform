using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class ConstraintIntegrationTests : BunitTestContext
{
    private void RegisterSlug() =>
        Services.Configure<BrouterOptions>(o => o.Constraints.Register("slug",
            new BrouterTypeRouteConstraint<string>((string s, out string r) =>
            {
                r = s;
                return s.Length >= 3 && s.All(c => char.IsLetterOrDigit(c) || c == '-');
            })));

    [TestMethod]
    public void Container_scoped_constraint_flows_from_options_into_route_matching()
    {
        RegisterSlug();

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/posts/hello-world");

        var cut = RenderComponent<ConstraintHost>();

        // The route matched only because Broute resolved "slug" against BrouterOptions.Constraints.
        cut.WaitForAssertion(() => Assert.AreEqual("hello-world", cut.Find("[data-testid=post]").TextContent));
    }

    [TestMethod]
    public void Container_scoped_constraint_rejects_a_non_matching_value()
    {
        RegisterSlug();

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/posts/ab"); // too short: fails the slug constraint

        var cut = RenderComponent<ConstraintHost>();

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=post]").Count));
    }
}
