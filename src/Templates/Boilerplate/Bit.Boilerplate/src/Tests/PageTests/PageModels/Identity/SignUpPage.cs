//+:cnd:noEmit
using Boilerplate.Tests.PageTests.PageModels.Email;
using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignUpPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    private string? email;
    public override string PagePath => Urls.SignUpPage;
    public override string PageTitle => AppStrings.SignUpPageTitle;

    public override async Task Open()
    {
        await base.Open();

        //#if (captcha == "reCaptcha")
        //Override behavior of the javascript recaptcha function on the browser
        await Page.WaitForFunctionAsync("window.grecaptcha?.getResponse !== undefined");
        await Page.EvaluateAsync("window.grecaptcha.getResponse = () => 'not-empty';");
        //#endif
    }

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.SignUp);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true })).ToBeVisibleAsync();
    }

    public async Task<ConfirmPage> SignUp(string email, string password = TestData.DefaultTestPassword)
    {
        this.email = email;
        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true }).ClickAsync();

        return new(Page, WebAppServerAddress) { EmailAddress = email };
    }

    public async Task<ConfirmationEmail<ConfirmPage>> OpenConfirmationEmail()
    {
        Assert.IsNotNull(email, $"Call {nameof(SignUp)} method first.");

        var confirmationEmail = new ConfirmationEmail<ConfirmPage>(Page.Context, WebAppServerAddress);
        await confirmationEmail.Open(email);
        return confirmationEmail;
    }
}
