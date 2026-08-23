using Bunit;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// SignInPanel keeps the credentials it is going to submit in a single <see cref="SignInRequestDto"/> that
/// outlives any one attempt, and its identifier fields are seeded from the query string by a magic link. The
/// server then picks WHICH credential it is looking at from those fields alone - <c>isOtpSignIn</c> is just
/// <c>string.IsNullOrEmpty(request.Otp) is false</c>, and <c>UserManagerExtensions.FindUser</c> resolves a user
/// name ahead of an e-mail - so a field left behind from an earlier attempt, or planted in the url, silently
/// changes the meaning of the next submit.
///
/// The two defects that produced this file were closed at different layers, and each test asserts at the layer
/// that now carries the invariant: the spent one-time code is cleared by the panel, while the user name is no
/// longer emitted by the server at all (which is what let the client drop the parameter entirely). Both fail on
/// the pre-fix code; neither asserts on a private field.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class SignInPanelCredentialStateTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Pins BP-408. A spent or wrong <c>?otp=</c> stays in the model after its auto-submit fails, and because
    /// the panel is in its Full layout there is no OTP box on screen for the user to clear it from. Every later
    /// submit therefore reaches the server as an OTP sign-in and the typed password is never read.
    /// </summary>
    [TestMethod]
    public async Task SignInPanel_Should_ClearASpentOtp_SoThatPasswordSignInStillWorks()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(CancellationToken.None);

        await using var ctx = server.CreateBunitContext();

        // What makes the code below spent is that the seeded account has no live one-time code - OtpRequestedOn is
        // persisted per user and the test database outlives the run, so an unrelated test (or an earlier run of one)
        // that issued a code for this account would otherwise turn the rejection below from ExpiredToken into
        // InvalidUserCredentials, and the wait would sit here until it timed out.
        await TestAccountUtils.ResetOtpAndLockoutState(server, TestData.DefaultTestEmail, CancellationToken.None);

        // The auto-submit is still in flight when Render() returns - it yields at its first await - and DoSignIn
        // has no re-entrancy guard, so typing on top of it would make this a test about timing instead of about
        // the spent code. Its rejection raises an error snack bar, which is a deterministic completion signal:
        // SnackBarService publishes with persistent: true, so the message is delivered even if the round trip
        // finishes before this subscription is installed. The delegate is held in a local because PubSubService
        // keeps only a weak reference to it.
        string? lastSnackBar = null;
        var autoSubmitRejected = new TaskCompletionSource();
        Func<object?, Task> onSnackBar = payload =>
        {
            // Match the rejection specifically. Any other snack bar (a transient network warning, say) would
            // otherwise release the wait while the sign-in round trip is still running.
            if (payload is ValueTuple<string, string, BitColor> snack)
            {
                lastSnackBar = snack.Item1; // Only to make a timeout below say what actually came back.

                if (snack.Item1 == AppStrings.ExpiredToken)
                {
                    autoSubmitRejected.TrySetResult();
                }
            }

            return Task.CompletedTask;
        };
        var unsubscribe = ctx.Services.GetRequiredService<PubSubService>()
                                     .Subscribe(ClientAppMessages.SHOW_SNACK, onSnackBar);

        // Arrive the way a re-opened magic link arrives: an identifier plus a one-time code that is no longer
        // valid. The query has to be driven through the NavigationManager - bUnit refuses to have
        // [SupplyParameterFromQuery] properties set as ordinary parameters, and going through the url is also
        // the only form that exercises the real binding.
        ctx.Services.GetRequiredService<NavigationManager>()
           .NavigateTo($"{PageUrls.SignIn}?email={Uri.EscapeDataString(TestData.DefaultTestEmail)}&otp=000000");

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SignInPanel>(panel => panel
                .Add(p => p.SignInPanelType, SignInPanelType.Full)
                .Add(p => p.ReturnUrl, PageUrls.Settings)));

        try
        {
            await autoSubmitRejected.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            Assert.Fail($"The auto-submitted one-time code should have been rejected as expired. Last snack bar: " +
                        $"'{lastSnackBar ?? "<none>"}'. '{AppStrings.InvalidUserCredentials}' here means the seeded " +
                        $"account had a live one-time code despite the reset above - find whoever issued it.");
        }

        unsubscribe();
        GC.KeepAlive(onSnackBar);

        // Now do what the user does next: type the correct password and submit. This must be treated as a
        // PASSWORD sign-in. Before the fix the spent Otp was still in the model, so the server took the OTP
        // branch and this attempt failed exactly like the first one.

        // The e-mail is already on the model from the query string; only the password has to be typed. Its field
        // is not debounced, so .Change() binds it synchronously.
        cut.Find($"input[placeholder='{AppStrings.PasswordPlaceholder}']").Change(TestData.DefaultTestPassword);
        cut.Find("form").Submit();

        var authenticationStateProvider = ctx.Services.GetRequiredService<AuthenticationStateProvider>();

        await cut.WaitForAssertionAsync(async () =>
        {
            var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
            Assert.IsTrue(user.IsAuthenticated(),
                "A failed magic link must not turn every later password sign-in on this page into an OTP sign-in.");
        }, timeout: TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Pins BP-409, at the place the invariant now lives.
    ///
    /// The magic link must identify its recipient by the address it was delivered to. It used to carry
    /// <c>userName=</c>, and <c>UserManagerExtensions.FindUser</c> resolves a user name <b>before</b> the e-mail
    /// or phone number the visitor typed - so a link (or any hand-made url) carrying an existing user name
    /// silently redirected the request to that account, and on a first-time visitor it also became the user name
    /// of the account auto-provisioning created for them.
    ///
    /// Asserting on the emitted link rather than on the sign-in page is deliberate: the client no longer reads a
    /// <c>userName</c> query parameter at all, so a page-level test could only prove that an unknown parameter is
    /// ignored, which would keep passing if the server started emitting one again.
    /// </summary>
    [TestMethod]
    public async Task MagicLink_Should_IdentifyTheRecipientByEmail_NotByUserName()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        // A per-run account, not the shared seeded one. SendOtp stamps OtpRequestedOn on whoever it is called for and
        // that state outlives the run, so issuing a code for test@bitplatform.dev here would leave a live magic link
        // on the account the sibling test above needs to have none - and MSTest runs two test methods at a time, so
        // the two would race even within a single run. It also re-arms SendOtp's own resend throttle. See
        // TestAccountUtils. CreateAndSignIn leaves the account e-mail confirmed, which is what makes the call below
        // send a plain OTP e-mail carrying the magic link rather than a confirmation e-mail.
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken);

        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

        var query = captured.Link!.Query;

        Assert.IsFalse(query.Contains("userName=", StringComparison.OrdinalIgnoreCase),
            $"The magic link must not carry a user name - FindUser resolves it ahead of the identifier the visitor types. Query was: {query}");

        Assert.IsTrue(query.Contains($"email={Uri.EscapeDataString(email)}", StringComparison.OrdinalIgnoreCase),
            $"The magic link must identify its recipient by the address it was delivered to. Query was: {query}");
    }

    /// <summary>
    /// BP-412 is OPEN, so this test is ignored rather than deleted - it is the executable form of the finding.
    ///
    /// <c>?error=</c> is echoed verbatim into an error snack bar, so any URL on the real domain can put
    /// attacker-authored text on screen as the application's own message. It cannot be fixed on the client
    /// alone: <c>IdentityController.ExternalSignIn</c> is the only legitimate producer and it emits free-form
    /// localized prose, not a key, so a client-side allow-list would blank out every genuine external-sign-in
    /// failure message.
    ///
    /// To unblock: change the server to emit a resource KEY (<c>?error=ExternalSignInFailed</c>), then resolve
    /// it here only for keys in a known set and fall back to AppStrings.UnknownException. Both halves must land
    /// in one commit. Remove [Ignore] at that point - the assertion below is already the right one.
    /// </summary>
    [TestMethod, Ignore("BP-412 is open: needs the coordinated server change to error KEYS first. See the summary.")]
    public async Task SignInPanel_Should_NotDisplayArbitraryTextFromTheErrorQueryString()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(CancellationToken.None);

        await using var ctx = server.CreateBunitContext();

        const string attackerText = "Your account was suspended. Call +1-555-0100 to restore access.";

        // The snack bar is rendered by AppSnackBar in the layout, NOT inside this panel, so asserting on
        // cut.Markup would pass no matter what the panel does. What the panel actually controls - and what the
        // finding is about - is the message it publishes, so that is what is asserted.
        string? shownTitle = null;
        Func<object?, Task> onSnackBar = payload =>
        {
            if (payload is ValueTuple<string, string, BitColor> snack) { shownTitle = snack.Item1; }
            return Task.CompletedTask;
        };
        var unsubscribe = ctx.Services.GetRequiredService<PubSubService>()
                                     .Subscribe(ClientAppMessages.SHOW_SNACK, onSnackBar);

        ctx.Services.GetRequiredService<NavigationManager>()
           .NavigateTo($"{PageUrls.SignIn}?error={Uri.EscapeDataString(attackerText)}");

        ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SignInPanel>(panel => panel
                .Add(p => p.SignInPanelType, SignInPanelType.Full)));

        await Task.Delay(TimeSpan.FromSeconds(1));
        unsubscribe();
        GC.KeepAlive(onSnackBar);

        Assert.AreNotEqual(attackerText, shownTitle,
            "Text taken straight from the query string must not be shown as an application message.");
    }
}
