using System.Text.RegularExpressions;
using Boilerplate.Tests.PageTests.PageModels.Email;
using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class ForgotPasswordPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    private string? email;
    public override string PagePath => Urls.ForgotPasswordPage;
    public override string PageTitle => AppStrings.ForgotPasswordTitle;

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ForgotPasswordTitle);
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ForgotPasswordMessage);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordMessageInForgot);
        var resetPasswordLink = Page.GetByRole(AriaRole.Link, new() { Name = AppStrings.ResetPassword });
        await Assertions.Expect(resetPasswordLink).ToBeVisibleAsync();
        await Assertions.Expect(resetPasswordLink).ToHaveAttributeAsync("href", Urls.ResetPasswordPage);
    }

    public async Task<ResetPasswordPage> ForgotPassword(string email = TestData.DefaultTestEmail)
    {
        this.email = email;
        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();

        return new(Page, WebAppServerAddress) { EmailAddress = email };
    }

    public async Task AssertUserNotFound()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.UserNotFound)).ToBeVisibleAsync();
    }

    public async Task AssertTooManyRequests()
    {
        var pattern = new Regex(AppStrings.WaitForResetPasswordTokenRequestResendDelay.Replace("{0}", ".*"));
        await Assertions.Expect(Page.GetByText(pattern)).ToBeVisibleAsync();
    }

    public async Task<ResetPasswordEmail> OpenResetPasswordEmail()
    {
        Assert.IsNotNull(email, $"Call {nameof(ForgotPassword)} method first.");

        var resetPasswordEmail = new ResetPasswordEmail(Page.Context, WebAppServerAddress);
        await resetPasswordEmail.Open(email);
        return resetPasswordEmail;
    }
}
