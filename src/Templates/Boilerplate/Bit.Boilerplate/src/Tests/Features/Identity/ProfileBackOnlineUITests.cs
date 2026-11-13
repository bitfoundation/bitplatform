namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class ProfileBackOnlineUITests : AppPageTest
{
    /// <summary>
    /// The signed-in user's profile, which <c>MainLayout</c> loads when the app starts, is loaded again once the app is
    /// back online when the server couldn't be reached for it then, as when the app is opened offline:
    /// <list type="number">
    /// <item>Sign in, then open the Settings page again while the call that loads the profile can't reach the server.
    /// The Profile section has no profile to show.</item>
    /// <item>Let that call through again, and have the app reach the server with another call, the Sessions section's.</item>
    /// <item>The Profile section shows the user's profile, with no reload.</item>
    /// </list>
    /// It runs in Blazor WebAssembly mode, where the browser itself makes the app's calls, so a failed one is a call
    /// that couldn't reach the server, as it is for an app opened offline.
    /// </summary>
    [TestMethod]
    public async Task Profile_Should_Load_OnceTheAppIsBackOnline()
    {
        const string getCurrentUser = "**/api/v1/User/GetCurrentUser**";

        await using var server = new AppTestServer(Context);
        var serverAddress = server.WebAppAddress;

        await server.Build(configureTestConfiguration: configuration => configuration["WebAppRender:BlazorMode"] = nameof(BlazorWebAppMode.BlazorWebAssembly))
            .Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SignInPanelUtils.FillCredentials(Page, TestData.DefaultTestEmail, TestData.DefaultTestPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(serverAddress.ToString());

        // The app starts again, and its call for the profile can't reach the server
        var profileCallFailed = new TaskCompletionSource();
        await Page.RouteAsync(getCurrentUser, async route =>
        {
            await route.AbortAsync();
            profileCallFailed.TrySetResult();
        });
        await Page.GotoAsync(new Uri(serverAddress, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Profile}").ToString());
        await profileCallFailed.Task.WaitAsync(TimeSpan.FromSeconds(60), TestContext.CancellationToken);
        await Page.WaitForBlazorInteractive();

        var fullName = Page.GetByLabel(AppStrings.FullName, new() { Exact = true });
        await Expect(fullName).ToHaveCountAsync(0); // Only the placeholder, as the profile is missing

        // The server can be reached again, and the Sessions section's call shows the app it's back online
        await Page.UnrouteAsync(getCurrentUser);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SessionsTitle }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ProfileTitle }).ClickAsync();

        await Expect(fullName).ToHaveValueAsync(TestData.DefaultTestFullName);
    }
}
