using Bit.Brouter;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Bit.Brouter.Tests;

public class GuardAndLoaderTests : TestContext
{
    public GuardAndLoaderTests()
    {
        Services.AddBitBrouterServices();
    }

    [Fact]
    public void Guard_can_redirect()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/secret");

        var cut = RenderComponent<GuardHost>();

        cut.WaitForAssertion(() => Assert.EndsWith("/login", nav.Uri));
    }

    [Fact]
    public void Loader_value_is_exposed_via_RouteData()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/data");

        var cut = RenderComponent<LoaderHost>();
        cut.WaitForAssertion(() => Assert.Equal("loaded!", cut.Find("[data-testid=val]").TextContent));
    }
}
