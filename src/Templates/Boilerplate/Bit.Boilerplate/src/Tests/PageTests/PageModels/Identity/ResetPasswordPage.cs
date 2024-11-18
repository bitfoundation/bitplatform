using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class ResetPasswordPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    public override string PagePath => Urls.ResetPasswordPage;
    public override string PageTitle => AppStrings.ResetPasswordTitle;
    public string? EmailAddress { private get; init; }

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPassword);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordSubtitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordMessage);
        var emailInput = Page.GetByPlaceholder(AppStrings.EmailPlaceholder);
        if (EmailAddress is null)
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeEnabledAsync();
            await Assertions.Expect(emailInput).ToBeEditableAsync();
        }
        else
        {
            await Assertions.Expect(emailInput).ToBeVisibleAsync();
            await Assertions.Expect(emailInput).ToBeDisabledAsync();
            await Assertions.Expect(emailInput).Not.ToBeEditableAsync();
        }
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.TokenPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.NotReceivedEmailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.CheckSpamMailMessage);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Resend })).ToBeVisibleAsync();
    }

    public async Task ContinueByToken(string token, string? email = null)
    {
        Assert.IsTrue(EmailAddress is not null || email is not null, "Either email address from query string or email input is required");
        Assert.IsTrue(EmailAddress is null || email is null, "Both email address from query string and email input cannot be used at the same time");

        if (email is not null)
            await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);

        await Page.GetByPlaceholder(AppStrings.TokenPlaceholder).FillAsync(token);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue }).ClickAsync();
    }

    public async Task AssertInvalidToken()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.InvalidToken)).ToBeVisibleAsync();
    }

    public async Task AssertValidToken()
    {
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.ConfirmPassword)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResetPasswordButtonText })).ToBeVisibleAsync();
    }

    public async Task SetPassword(string newPassword)
    {
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(newPassword);
        await Page.GetByPlaceholder(AppStrings.ConfirmPassword).FillAsync(newPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResetPasswordButtonText }).ClickAsync();
    }

    public async Task AssertSetPassword()
    {
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordSuccessTitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordSuccessBody);
    }
}
