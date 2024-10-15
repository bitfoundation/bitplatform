//+:cnd:noEmit
using Boilerplate.Tests.PageTests.PageModels.Email;
using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class ForgotPasswordPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress, Urls.ForgotPasswordPage, AppStrings.ForgotPasswordTitle)
{
    private string emailAddress;

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ForgotPassword);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ForgotPasswordMessage);
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.ResetPasswordMessageInForgot);
        var resetPasswordLink = page.GetByRole(AriaRole.Link, new() { Name = AppStrings.ResetPassword });
        await Assertions.Expect(resetPasswordLink).ToBeVisibleAsync();
        await Assertions.Expect(resetPasswordLink).ToHaveAttributeAsync("href", Urls.ResetPasswordPage);
    }

    public async Task<ResetPasswordPage> ForgotPassword(string email = "test@bitplatform.dev")
    {
        emailAddress = email;

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(emailAddress);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();

        return new(page, serverAddress, emailAddress);
    }

    public async Task<ResetPasswordEmail> OpenResetPasswordEmail()
    {
        var resetPasswordEmail = new ResetPasswordEmail(page.Context, serverAddress);
        await resetPasswordEmail.Open(emailAddress);
        return resetPasswordEmail;
    }
}
