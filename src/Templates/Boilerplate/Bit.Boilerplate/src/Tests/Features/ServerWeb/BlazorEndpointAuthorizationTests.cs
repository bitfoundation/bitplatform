//+:cnd:noEmit
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// <c>MapRazorComponents</c> copies each page component's <c>[Authorize]</c> attributes into its endpoint metadata, and
/// <c>ConfigureMiddlewares</c> calls <c>blazorApp.AllowAnonymous()</c> to switch that off. Doing so is correct exactly
/// when Blazor produces no server-side output for the request, because then there is nothing for endpoint authorization
/// to protect and the interactive client enforces it instead.
/// <para>
/// This is a pair on purpose. The first test proves the relaxation still happens where it is meant to (deleting it
/// would make an anonymous caller unable to fetch even the empty shell of a protected page). The second proves it stays
/// out of <c>BlazorSsr</c>, where the whole component tree IS rendered on the server - there the only remaining gate is
/// <c>AuthorizeRouteView</c>, whose principal comes from <c>ParseAccessToken</c>, which does no signature verification
/// and reads the <c>access_token</c> cookie straight off the request.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Security")]
public partial class BlazorEndpointAuthorizationTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task BlazorEndpoints_Should_AllowAnonymous_WhenNothingIsRenderedOnTheServer()
    {
        Assert.IsTrue(await BlazorEndpointsAreAnonymous(blazorMode: "BlazorServer", prerenderEnabled: "false"),
            "Nothing is produced server side in this configuration, so endpoint authorization only prevents the client " +
            "from receiving the page shell it is supposed to render itself.");
    }

    /// <summary>
    /// ⚠ <c>[Ignore]</c>d because BP-258 is <b>open</b>: the guard at <c>Program.Middlewares.cs</c> keys on
    /// <c>PrerenderEnabled</c> alone, so <c>BlazorSsr</c> still drops endpoint authorization while rendering the whole
    /// component tree on the server. Remove the attribute once the condition also requires
    /// <c>settings.WebAppRender.RenderMode is not null</c>. Without the attribute it fails on the first assertion with
    /// "Expected condition to be false" - i.e. every razor component endpoint carries <c>IAllowAnonymous</c> under
    /// <c>BlazorSsr</c>.
    /// </summary>
    [TestMethod, Ignore]
    public async Task BlazorEndpoints_Should_KeepAuthorization_WhenTheComponentTreeRendersOnTheServer()
    {
        Assert.IsFalse(await BlazorEndpointsAreAnonymous(blazorMode: "BlazorSsr", prerenderEnabled: "false"),
            "BlazorSsr renders every page on the server. Dropping [Authorize] from the endpoints leaves an unsigned, " +
            "unverified access_token cookie as the only thing between an anonymous caller and a protected page's markup.");

        // The same must hold when pre-rendering is on, which is the case the original condition already covered.
        Assert.IsFalse(await BlazorEndpointsAreAnonymous(blazorMode: "BlazorServer", prerenderEnabled: "true"));
    }

    /// <summary>
    /// Reads the razor-component endpoints the app actually mapped and reports whether they carry
    /// <see cref="IAllowAnonymous"/> metadata - i.e. whether <c>AuthorizationMiddleware</c> will skip their
    /// <c>[Authorize]</c> attributes.
    /// </summary>
    private async Task<bool> BlazorEndpointsAreAnonymous(string blazorMode, string prerenderEnabled)
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices(),
            configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:BlazorMode"] = blazorMode;
                configuration["WebAppRender:PrerenderEnabled"] = prerenderEnabled;
            }).Start(TestContext.CancellationToken);

        var componentEndpoints = server.WebApp.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>() is not null)
            .ToArray();

        // Non-vacuity: without this, an empty endpoint set would make Any() false and the second test pass for the
        // wrong reason.
        Assert.IsGreaterThan(0, componentEndpoints.Length,
            $"No razor component endpoints were mapped for BlazorMode '{blazorMode}' - the assertion below would be vacuous.");

        return componentEndpoints.All(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null);
    }
}
