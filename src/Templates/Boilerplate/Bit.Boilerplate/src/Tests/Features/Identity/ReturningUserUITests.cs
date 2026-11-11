using Microsoft.AspNetCore.Identity;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// A signed in user who opens the app again sees their account from the first moments of the start, from what the device
/// kept of it next to its tokens, until the server answers; and nothing of it stays on the device after a sign out
/// (See AuthManager.StoreActiveAccount).
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class ReturningUserUITests : AppPageTest
{
    /// <summary>
    /// The server is held from answering who the user is, so the name in the header can only come from what the device
    /// kept of the account; meanwhile the user's name changed on the server, as an edit on another device would change it.
    /// Prerendered, the server would answer with the page itself, which the held answer would hold up, so this test pins
    /// the client's own start.
    /// </summary>
    [TestMethod]
    public async Task AReturningUser_Should_SeeTheirAccountAtOnce_AndTheServersLatestWhenItAnswers()
    {
        var gate = new RequestGate();

        await using var server = new AppTestServer(Context);
        await server.Build(configureTestApiAppServices: services => services.AddRequestGate(gate),
                           configureTestConfiguration: configuration => configuration["WebAppRender:PrerenderEnabled"] = "false")
                    .Start(TestContext.CancellationToken);

        var email = MagicLinkSignInUtils.NewTestEmail();
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);

        const string fullName = "Returning Scenario";
        await SetFullName(server, email, fullName);

        gate.Hold("/api/v1/User/GetCurrentUser");
        try
        {
            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });

            await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
        }
        finally
        {
            gate.Release();
        }

        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(fullName);
    }

    [TestMethod]
    public async Task SigningOut_Should_LeaveNothingOfTheAccountOnTheDevice()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var email = MagicLinkSignInUtils.NewTestEmail();
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);

        await AppMenuUtils.ClickItem(Page, AppStrings.SignOut);
        await Expect(Page.GetByText(AppStrings.SignOutPrompt)).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).Last.ClickAsync();

        await Expect(Page.Locator("header .bit-drm-btn").First).Not.ToContainTextAsync(email);

        var keptOnTheDevice = await Page.EvaluateAsync<string>("() => JSON.stringify({ ...localStorage }) + JSON.stringify({ ...sessionStorage }) + document.cookie");

        Assert.IsFalse(keptOnTheDevice.Contains(email, StringComparison.OrdinalIgnoreCase), keptOnTheDevice);
    }

    /// <summary>Changes the user's full name on the server, the way an edit made on another device would.</summary>
    private static async Task SetFullName(AppTestServer server, string email, string fullName)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException($"No user has the e-mail {email}.");
        user.FullName = fullName;

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));
    }
}
