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
/// Confirming an identifier ends with an automatic sign-in, and that sign-in answers
/// <c>{ requiresTwoFactor: true }</c> - HTTP 200, with <b>null</b> tokens and no session row - whenever the account has
/// two-factor enabled. <c>ConfirmEmailRequestDto</c> carries no second-factor code, so the flow cannot be completed
/// from this page at all; the only correct behaviour is to confirm and stop.
/// <para>
/// Storing that response instead is the failure this pins, and it is worse than it looks: on the Web client the null
/// crosses JS interop and <c>Storage.setItem</c> coerces it, so <c>access_token</c> becomes the literal string
/// <c>"null"</c>, which is non-empty enough to pass the token provider's guard and then throws while being parsed - an
/// interrupting error dialog on every auth-state evaluation until the tokens are cleared, on top of the sign-out.
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

        // The confirmation itself must still happen - this is not "reject the request", it is "do not pretend it
        // signed you in".
        await cut.WaitForAssertionAsync(async () =>
        {
            Assert.IsTrue(await IsEmailConfirmed(server, email), "The e-mail must still be confirmed.");
        }, timeout: TimeSpan.FromSeconds(30));

        Assert.AreEqual(ExistingSessionMarker, await storageService.GetItem("access_token"),
            "A requiresTwoFactor response carries null tokens; storing it would overwrite the caller's live session.");

        Assert.AreEqual(navigationManager.ToAbsoluteUri(confirmUrl).ToString(), navigationManager.Uri,
            "Without tokens the user is not signed in, so the page must not navigate away as if it had succeeded.");
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
