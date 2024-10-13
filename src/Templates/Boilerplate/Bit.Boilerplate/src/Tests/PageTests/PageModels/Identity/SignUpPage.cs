//+:cnd:noEmit
using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Tests.PageTests.PageModels.Layout;
using MsgReader.Mime;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignUpPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress, Urls.SignUpPage, AppStrings.SingUpTitle)
{
    private string email;
    private IPage emailPage;

    public override async Task Open()
    {
        await base.Open();

        //#if (captcha == "reCaptcha")
        //Override behavior of the javascript recaptcha function on the browser
        await page.WaitForFunctionAsync("window.grecaptcha?.getResponse !== undefined");
        await page.EvaluateAsync("window.grecaptcha.getResponse = () => 'not-empty';");
        //#endif
    }

    public async Task SignUp()
    {
        email = $"{Guid.NewGuid()}@gmail.com";

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync("123456");
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true }).ClickAsync();
    }

    public async Task AssertSignUp()
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText })).ToBeVisibleAsync();
    }

    public async Task OpenEmail()
    {
        var html = ReadEmlAndGetHtmlBody(email);
        emailPage = await page.Context.NewPageAsync();
        await emailPage.SetContentAsync(html);
    }

    public async Task AssertConfirmationEmailContent()
    {
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.WelcomeToApp);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(EmailStrings.EmailConfirmationMessageSubtitle.Replace("{0}", ".*")));
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyToken);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyLink);
    }

    public async Task ConfirmByOtp()
    {
        var optCode = await emailPage.GetByText(new Regex("^\\d{6}$")).TextContentAsync();
        await page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder).FillAsync(optCode!);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText }).ClickAsync();
    }

    public async Task ConfirmByMagicLink()
    {
        var optLink = emailPage.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ConfirmPage).ToString() });
        await Assertions.Expect(optLink).ToBeVisibleAsync();
        await optLink.ClickAsync();
        page = emailPage;
    }

    public async Task AssertConfirm()
    {
        await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = email })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bit-prs").First).ToContainTextAsync(email);
        await Assertions.Expect(page.Locator(".bit-prs").Last).ToContainTextAsync(email);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    private static string ReadEmlAndGetHtmlBody(string toMailAddress)
    {
        var email = LoadLastEmailFor(toMailAddress);

        Assert.IsNotNull(email);
        Assert.AreEqual("info@Boilerplate.com", email.Headers.From.Address);
        Assert.AreEqual(EmailStrings.DefaultFromName, email.Headers.From.DisplayName);
        Assert.IsTrue(Regex.IsMatch(email.Headers.Subject, EmailStrings.ConfirmationEmailSubject.Replace("{0}", "\\d{6}")), "Email subject does not match.");

        return email.HtmlBody.GetBodyAsText();
    }

    private static Message? LoadLastEmailFor(string toMailAddress)
    {
        var emailsDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "sent-emails");
        var messages = new DirectoryInfo(emailsDirectory).GetFiles().Select(Message.Load);
        return messages
            .Where(m => m.Headers.To[0].Address == toMailAddress)
            .MaxBy(m => m.Headers.DateSent);
    }
}
