//+:cnd:noEmit
using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using MsgReader.Mime;

namespace Boilerplate.Tests.PageTests.PageModels;

public partial class SignUpPage(IPage page, Uri serverAddress)
{
    public async Task Open()
    {
        var response = await page.GotoAsync(new Uri(serverAddress, Urls.SignUpPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Assertions.Expect(page).ToHaveTitleAsync(AppStrings.SingUpTitle);

        //#if (captcha == "reCaptcha")
        //Override behavior of the javascript recaptcha function on the browser
        await page.WaitForFunctionAsync("window.grecaptcha?.getResponse !== undefined");
        await page.EvaluateAsync("window.grecaptcha.getResponse = () => 'not-empty';");
        //#endif
    }

    public async Task SignUp(bool usingMagicLink = true)
    {
        var email = $"{Guid.NewGuid()}@gmail.com";

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync("123456");
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignUp, Exact = true }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText })).ToBeVisibleAsync();

        var html = ReadEmlAndGetHtmlBody(email);
        var emailPage = await page.Context.NewPageAsync();
        await emailPage.SetContentAsync(html);

        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.WelcomeToApp);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(EmailStrings.EmailConfirmationMessageSubtitle.Replace("{0}", ".*")));
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyToken);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyLink);

        IPage confirmPage;
        if (usingMagicLink)
        {
            var optLink = emailPage.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ConfirmPage).ToString() });
            await Assertions.Expect(optLink).ToBeVisibleAsync();
            await optLink.ClickAsync();
            confirmPage = emailPage;
        }
        else
        {
            var optCode = await emailPage.GetByText(new Regex("^\\d{6}$")).TextContentAsync();
            await page.GetByPlaceholder(AppStrings.EmailTokenPlaceholder).FillAsync(optCode!);
            await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EmailTokenConfirmButtonText }).ClickAsync();
            confirmPage = page;
        }

        await Assertions.Expect(confirmPage).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(confirmPage.GetByRole(AriaRole.Button, new() { Name = email })).ToBeVisibleAsync();
        await Assertions.Expect(confirmPage.GetByText(email).First).ToBeVisibleAsync();
        await Assertions.Expect(confirmPage.GetByText(email).Nth(1)).ToBeVisibleAsync();
        await Assertions.Expect(confirmPage.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Assertions.Expect(confirmPage.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync(new() { Visible = false });

        await emailPage.CloseAsync();
    }

    private string ReadEmlAndGetHtmlBody(string toMailAddress)
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
