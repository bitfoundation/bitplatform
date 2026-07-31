using Bunit;
using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Tests.Infrastructure.Services;
using Boilerplate.Server.Api.Features.Identity.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages.Identity;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Confirming an identifier ends with an automatic sign-in, and that sign-in answers a two-factor challenge - HTTP 200
/// with <b>no</b> tokens - whenever the account has two-factor enabled. Not being signed in there is fine and expected;
/// the user simply signs in again. What must not happen is that empty response being <b>stored</b>.
/// <para>
/// That is why <c>AuthManager.StoreTokens</c> ignores a response without an access token, and it is worse than a plain
/// sign-out: on the Web client the null crosses JS interop and <c>Storage.setItem</c> coerces it, so
/// <c>access_token</c> becomes the literal string <c>"null"</c> - non-empty enough to pass the token provider's guard
/// and then throwing while being parsed, which surfaces as an interrupting error dialog on every auth-state
/// evaluation until the tokens are cleared.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest")]
public class ConfirmPageTwoFactorTests
{
    [TestMethod]
    public async Task ConfirmEmail_Should_NotStoreTokens_When_TheAccountRequiresASecondFactor()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        var (email, token) = await CreateUnconfirmedTwoFactorAccount(server);

        await using var ctx = server.CreateBunitContext();

        var storageService = ctx.Services.GetRequiredService<IStorageService>();
        await storageService.SetItem("access_token", ExistingSessionMarker);

        // [SupplyParameterFromQuery] values cannot be passed as bUnit parameters - the query string has to be real, so
        // navigate to the confirmation link first. That is also exactly what clicking the e-mailed link does, and
        // ConfirmPage.OnInitAsync then auto-submits ConfirmEmail.
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var confirmUrl = $"{PageUrls.Confirm}?email={Uri.EscapeDataString(email)}&emailToken={Uri.EscapeDataString(token)}";
        navigationManager.NavigateTo(confirmUrl);

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters.AddChildContent<ConfirmPage>());

        // Wait for the navigation, not for the confirmation: EmailConfirmed is written server-side while ConfirmEmail
        // is still running, so it goes true before the response is even sent - reading the storage on that signal
        // would race StoreTokens and pass whether or not it overwrote anything. ConfirmPage navigates only after
        // StoreTokens has completed, so that is the signal that the client is done.
        var homeUri = navigationManager.ToAbsoluteUri(PageUrls.Home).ToString();
        cut.WaitForAssertion(() => Assert.AreEqual(homeUri, navigationManager.Uri,
            "ConfirmPage navigates once it has handled the response; without that this assertion would race it."),
            timeout: TimeSpan.FromSeconds(30));

        // The confirmation itself must still happen - the second factor is only in the way of the convenience sign-in.
        Assert.IsTrue(await IsEmailConfirmed(server, email), "The e-mail must still be confirmed.");

        Assert.AreEqual(ExistingSessionMarker, await storageService.GetItem("access_token"),
            "A two-factor challenge carries no tokens, and storing it would overwrite what this browser already had.");
    }

    /// <summary>
    /// Creates an account whose e-mail is still unconfirmed while two-factor authentication is on, and returns the
    /// confirmation code that was e-mailed to it. <c>TwoFactorEnabled</c> is set directly on the row because the
    /// shipped flow for turning it on (<c>UserController</c>) needs an authenticator app.
    /// </summary>
    private async Task<(string email, string token)> CreateUnconfirmedTwoFactorAccount(AppTestServer server)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var email = MagicLinkSignInUtils.NewTestEmail();

        // A brand-new address auto-provisions an unconfirmed account and mails it a confirmation code.
        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken));

        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        await using var dbScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();
        await dbContext.Set<User>()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.TwoFactorEnabled, true), TestContext.CancellationToken);

        return (email, captured.Token!);
    }

    private static async Task<bool> IsEmailConfirmed(AppTestServer server, string email)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();

        return await dbContext.Set<User>().AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.EmailConfirmed);
    }

    /// <summary>A recognisable stand-in for the token of a session that already exists in this browser.</summary>
    private const string ExistingSessionMarker = "existing-session-access-token";

    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;
}
