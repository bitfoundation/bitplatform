namespace Boilerplate.Tests.PageTests.PageModels;

public partial class SignInPage(IPage page, Uri serverAddress)
{
    public bool IsSignedIn { get; private set; }

    public async Task Open()
    {
        var response = await page.GotoAsync(new Uri(serverAddress, Urls.SignInPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Assertions.Expect(page).ToHaveTitleAsync(AppStrings.SignInTitle);
    }

    public async Task SignIn(string email = "test@bitplatform.dev", string password = "123456",
        string expectedFullName = "Boilerplate test account", bool isValidCredentials = true)
    {
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        if (isValidCredentials)
        {
            await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = expectedFullName })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(expectedFullName).First).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(expectedFullName).Nth(1)).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync(new() { Visible = false });

            IsSignedIn = true;
        }
        else
        {
            await Assertions.Expect(page.GetByText(AppStrings.InvalidUserCredentials)).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync(new() { Visible = false });
        }
    }

    public async Task SignOut()
    {
        if (IsSignedIn is false)
            throw new InvalidOperationException("You must sign-in first.");

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();

        IsSignedIn = false;

        await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync(new() { Visible = false });
    }
}
