namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>
/// TfaPanel stands in for a modal over the sign in form (See SignInPanel.HideTfaAndOtpPanels): going back from it
/// closes it and leaves the user on the sign in form - not on the previous page, and not out of a hybrid app.
/// <para>
/// The global admin's second factor is an authenticator app, so the password step mails nothing and, left unanswered,
/// signs nothing in. The Web, Windows and Android classes deriving from this decide what "back" is.
/// </para>
/// </summary>
public abstract class TwoFactorBackNavigationTestsBase : AppTestBase
{
    /// <summary>The platform's own way back.</summary>
    protected abstract Task GoBack(IPage page);

    /// <summary>
    /// Todo routes with Blazor's Router and AdminPanel with Brouter (See the dotnet new lines in
    /// .github/workflows/*.cd.yml), and the lock has to hold under both.
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    public virtual async Task GoingBackFromTheSecondFactor_Should_ReturnToTheSignInForm(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.ApiOf(app));
        var configuration = apiClient.Services.GetRequiredService<IConfiguration>();

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        // In app, so there is a page before the sign in page to go back to.
        await GoToWhenInteractive(page, PageUrls.SignIn);

        await SubmitCredentials(page, configuration["GlobalAdminEmail"]!, configuration["GlobalAdminPassword"]!);

        var tfaTitle = page.GetByText(AppStrings.TfaPanelTitle, new() { Exact = true });
        await Expect(tfaTitle).ToBeVisibleAsync();

        await GoBack(page);

        await Expect(tfaTitle).ToBeHiddenAsync();
        await Expect(page.GetByText(AppStrings.SignInPanelTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }
}
