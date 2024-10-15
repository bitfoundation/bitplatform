using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class ResetPasswordPage(IPage page, Uri serverAddress, string? emailAddress)
    : MainLayout(page, serverAddress, Urls.ResetPasswordPage, AppStrings.ResetPasswordTitle)
{
    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPassword);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordSubtitle);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordMessage);
        var emailInput = page.GetByPlaceholder(AppStrings.EmailPlaceholder);
        if (emailAddress is null)
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeEnabledAsync();
            await Assertions.Expect(emailInput).ToBeEditableAsync();
        }
        else
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeDisabledAsync();
            await Assertions.Expect(emailInput).ToBeEditableAsync(new() { Editable = false });
        }
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.TokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue })).ToBeVisibleAsync();
    }

    public async Task ContinueByToken(string? email, string token)
    {
        Assert.IsTrue(emailAddress is not null || email is not null, "Either email address from query string or email input is required");
        Assert.IsTrue(emailAddress is null || email is null, "Both email address from query string and email input cannot be used at the same time");

        if (email is not null)
            await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);

        await page.GetByPlaceholder(AppStrings.TokenPlaceholder).FillAsync(token);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue }).ClickAsync();
    }

    public async Task Continue()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue }).ClickAsync();
    }

    public async Task AssertInvalidToken()
    {
        await Assertions.Expect(page.GetByText(AppStrings.InvalidToken)).ToBeVisibleAsync();
    }

    public async Task AssertValidToken()
    {
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.ConfirmPassword)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResetPasswordButtonText })).ToBeVisibleAsync();
    }

    public async Task SetPassword(string newPassword)
    {
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(newPassword);
        await page.GetByPlaceholder(AppStrings.ConfirmPassword).FillAsync(newPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResetPasswordButtonText }).ClickAsync();
    }

    public async Task AssertSetPassword()
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordSuccessTitle);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordSuccessBody);
    }
}