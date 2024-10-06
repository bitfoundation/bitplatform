using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests.PageModels;

public partial class SignInPage(IPage page, Uri serverAddress, string culture)
{
    #region Resources
    private readonly IStringLocalizer<AppStrings> appLocalizer = StringLocalizerFactory.Create<AppStrings>(culture);

    private string signInTitle => appLocalizer[nameof(AppStrings.SignInTitle)];
    private string emailPlaceholder => appLocalizer[nameof(AppStrings.EmailPlaceholder)];
    private string passwordPlaceholder => appLocalizer[nameof(AppStrings.PasswordPlaceholder)];
    private string signIn => appLocalizer[nameof(AppStrings.SignIn)];
    private string signOut => appLocalizer[nameof(AppStrings.SignOut)];
    private string invalidUserCredentials => appLocalizer[nameof(AppStrings.InvalidUserCredentials)];
    #endregion

    public bool IsSignedIn { get; private set; }

    public async Task Goto()
    {
        var response = await page.GotoAsync(new Uri(serverAddress, Urls.SignInPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await page.ChangeCulture(culture);
        await Assertions.Expect(page).ToHaveTitleAsync(signInTitle);
    }

    public async Task SignIn(string email = "test@bitplatform.dev", string password = "123456",
        string expectedFullName = "Boilerplate test account", bool isValidCredentials = true)
    {
        await page.GetByPlaceholder(emailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(passwordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = signIn }).ClickAsync();

        if (isValidCredentials)
        {
            await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
            await Assertions.Expect(page.Locator(".persona")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".persona")).ToContainTextAsync(expectedFullName);
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = signOut })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = signIn })).ToBeVisibleAsync(new() { Visible = false });

            IsSignedIn = true;
        }
        else
        {
            await Assertions.Expect(page.GetByText(invalidUserCredentials)).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = signIn })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = signOut })).ToBeVisibleAsync(new() { Visible = false });
        }
    }

    public async Task SignOut()
    {
        Assert.IsTrue(IsSignedIn, "You must sign-in first.");

        await page.GetByRole(AriaRole.Button, new() { Name = signOut }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = signOut }).ClickAsync();

        IsSignedIn = false;

        await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = signIn })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = signOut })).ToBeVisibleAsync(new() { Visible = false });
    }
}
