using Bunit;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages.Identity.SignIn;
using Boilerplate.Client.Core.Components.Pages.Identity.SignUp;
using Boilerplate.Client.Core.Components.Pages.Identity.Components;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// A failed external sign-in reports a resource key, and only the external sign-in a page started may show the text
/// behind it: in the apps that callback passes the local server's nonce check, on the web it comes from the page's own
/// popup. The page's own <c>?error=</c> can come from anyone's link, so it only ever shows the generic error (See
/// <c>SignInPanelCredentialStateTests.SignInPanel_Should_NotDisplayArbitraryTextFromTheErrorQueryString</c>).
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class ExternalSignInErrorTests
{
    private const string CallbackUrl = $"{PageUrls.SignIn}?error={nameof(AppStrings.BadRequestException)}";

    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task SignInPanel_Should_ShowTheCallbacksMessage_OnlyForTheExternalSignInItStarted()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = CreateContext(server);
        var (shownTitles, keepAlive) = CaptureSnackBars(ctx);

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SignInPanel>(panel => panel
                .Add(p => p.SignInPanelType, SignInPanelType.Full)));

        ctx.Services.GetRequiredService<PubSubService>().Publish(ClientAppMessages.EXTERNAL_SIGN_IN_CALLBACK, CallbackUrl);
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);

        Assert.IsEmpty(shownTitles, "Before the user picks a provider the panel isn't listening, so a callback it didn't ask for shows nothing.");

        await cut.InvokeAsync(() => cut.FindComponent<ExternalIdentityProviders>().Instance.OnClick.InvokeAsync("Google"));

        Assert.AreEqual(AppStrings.BadRequestException, await WaitForSnackBar(shownTitles),
            "The callback of the external sign-in the panel started shows the text behind its key.");

        GC.KeepAlive(keepAlive);
    }

    [TestMethod]
    public async Task SignUpPage_Should_ShowTheCallbacksMessage_InsteadOfLeavingForTheSignInPage()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = CreateContext(server);
        var (shownTitles, keepAlive) = CaptureSnackBars(ctx);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(PageUrls.SignUp);

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters.AddChildContent<SignUpPage>());

        await cut.InvokeAsync(() => cut.FindComponent<ExternalIdentityProviders>().Instance.OnClick.InvokeAsync("Google"));

        Assert.AreEqual(AppStrings.BadRequestException, await WaitForSnackBar(shownTitles),
            "The callback of the external sign-up the page started shows the text behind its key.");

        Assert.AreEqual(PageUrls.SignUp, new Uri(navigationManager.Uri).AbsolutePath,
            "Going on to the callback's sign-in url would only show the generic error the url's own error gets.");

        GC.KeepAlive(keepAlive);
    }

    private static BunitContext CreateContext(AppTestServer server)
    {
        var ctx = server.CreateBunitContext();

        ctx.Services.AddScoped<IExternalNavigationService>(sp => new CallbackNavigationService(sp.GetRequiredService<PubSubService>()));

        return ctx;
    }

    private static (List<string> ShownTitles, Func<object?, Task> KeepAlive) CaptureSnackBars(BunitContext ctx)
    {
        var shownTitles = new List<string>();

        Func<object?, Task> onSnackBar = payload =>
        {
            if (payload is ValueTuple<string, string, BitColor> snack)
            {
                lock (shownTitles) shownTitles.Add(snack.Item1);
            }
            return Task.CompletedTask;
        };

        ctx.Services.GetRequiredService<PubSubService>().Subscribe(ClientAppMessages.SHOW_SNACK, onSnackBar);

        return (shownTitles, onSnackBar);
    }

    private async Task<string?> WaitForSnackBar(List<string> shownTitles)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            lock (shownTitles)
            {
                if (shownTitles.Count > 0)
                    return shownTitles[0];
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.CancellationToken);
        }

        return null;
    }

    /// <summary>Delivers the provider's answer at once, the way the popup or the local server does after a real round trip.</summary>
    private sealed class CallbackNavigationService(PubSubService pubSubService) : IExternalNavigationService
    {
        public Task NavigateTo(string url)
        {
            pubSubService.Publish(ClientAppMessages.EXTERNAL_SIGN_IN_CALLBACK, CallbackUrl);
            return Task.CompletedTask;
        }
    }
}
