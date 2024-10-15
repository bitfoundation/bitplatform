//+:cnd:noEmit
using Boilerplate.Tests.PageTests.PageModels.Email;
using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignUpPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress, Urls.SignUpPage, AppStrings.SingUpTitle)
{
    private string emailAddress;

    public override async Task Open()
    {
        await base.Open();

        //#if (captcha == "reCaptcha")
        //Override behavior of the javascript recaptcha function on the browser
        await page.WaitForFunctionAsync("window.grecaptcha?.getResponse !== undefined");
        await page.EvaluateAsync("window.grecaptcha.getResponse = () => 'not-empty';");
        //#endif
    }

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.SignUp);
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true })).ToBeVisibleAsync();
    }

    /// <param name="email">The email is optional, if not provided, a random email will be generated</param>
    public async Task<ConfirmPage> SignUp(string email, string password = "123456")
    {
        emailAddress = email;
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true }).ClickAsync();

        return new(page, serverAddress, email);
    }

    public async Task<ConfirmationEmail> OpenConfirmationEmail()
    {
        var confirmationEmail = new ConfirmationEmail(page.Context, serverAddress);
        await confirmationEmail.Open(emailAddress);
        return confirmationEmail;
    }
}
