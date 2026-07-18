using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class ExternalIdentityTests : AppPageTest
{
    // Seeded in the Keycloak dev realm (See Boilerplate.Server.AppHost/Infrastructure/Realms/dev-realm.json).
    private const string RealmUserName = "alice";
    private const string RealmPassword = "alice";

    /// <summary>
    /// A user signs in through the external Keycloak identity provider. She opens the Sign in page and clicks the
    /// "Enterprise SSO" (Keycloak) button, which pops open Keycloak's own hosted login page; she enters her realm
    /// credentials there and Keycloak redirects back, completing the sign-in and landing her on the home page as an
    /// authenticated user.
    /// <para>
    /// This is an advanced test that drives a real Keycloak server: the Aspire host boots the Keycloak container during
    /// assembly initialization (See <see cref="TestsAssemblyInitializer.RunAspireHost"/>). Keycloak isn't an
    /// <c>IResourceWithConnectionString</c>, so that startup doesn't block on it; this test - the only one that needs it -
    /// waits for it to become healthy and points its own test server at it via <c>KEYCLOAK_HTTP</c>.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingKeycloak()
    {
        // Wait for the Keycloak container to become healthy; by then its realm (with the seeded 'alice' user) has been
        // imported, so the OpenID Connect endpoints are ready to serve.
        await TestsAssemblyInitializer.AspireApp.ResourceNotifications
            .WaitForResourceHealthyAsync("keycloak", TestContext.CancellationToken);
        var keycloakUrl = TestsAssemblyInitializer.AspireApp.GetEndpoint("keycloak", "http").ToString().TrimEnd('/');

        await using var server = new AppTestServer(Context);
        // KEYCLOAK_HTTP is the same variable Aspire's WithReference(keycloak) injects in production; providing it makes
        // the server register the "Keycloak" external authentication scheme (See Server.Api's Program.Services.cs) and
        // render its sign-in button. Scoping it to this server keeps the other tests unaffected.
        await server.Build(configureTestConfigurations: config => config["KEYCLOAK_HTTP"] = keycloakUrl)
            .Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        await Page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The external providers load asynchronously (GetSupportedExternalAuthSchemes); the Keycloak button (its tooltip
        // is the localized "Enterprise SSO" text) only appears once the server reports the scheme. Clicking it opens
        // Keycloak's hosted login page in a popup window (See DefaultExternalNavigationService.NavigateTo for the web flow).
        var keycloakLogin = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await Page.GetByTitle(AppStrings.KeycloakSignInButtonText).ClickAsync();
        });
        keycloakLogin.SetDefaultTimeout((float)TimeSpan.FromSeconds(30).TotalMilliseconds);

        // Keycloak's standard username/password login form uses these stable element ids.
        await keycloakLogin.Locator("#username").FillAsync(RealmUserName);
        await keycloakLogin.Locator("#password").FillAsync(RealmPassword);
        await keycloakLogin.Locator("#kc-login").ClickAsync();

        // With valid credentials (this realm user has no required actions and the client needs no consent), Keycloak
        // redirects the popup back to the server's external sign-in callback, which hands the result to the opener window
        // through the web-interop page and closes the popup. The main page then completes the sign-in and redirects home.
        await Expect(Page).ToHaveURLAsync(serverAddress.ToString(),
            new() { Timeout = (float)TimeSpan.FromSeconds(30).TotalMilliseconds });

        // Keycloak users are created with their username as the full name (See IdentityController.ExternalSignInCallback),
        // so the header persona shows "alice" and the Sign in button is gone.
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(RealmUserName);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }
}
