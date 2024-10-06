using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Tests.Services;
using MsgReader.Mime;

namespace Boilerplate.Tests.PageTests.PageModels;

public partial class SignUpPage(IPage page, Uri serverAddress, string culture)
{
    #region Resources
    private readonly IStringLocalizer<AppStrings> appLocalizer = StringLocalizerFactory.Create<AppStrings>(culture);
    private readonly IStringLocalizer<EmailStrings> emailLocalizer = StringLocalizerFactory.Create<EmailStrings>(culture);

    private string emailPlaceholder => appLocalizer[nameof(AppStrings.EmailPlaceholder)];
    private string passwordPlaceholder => appLocalizer[nameof(AppStrings.PasswordPlaceholder)];
    private string singUpTitle => appLocalizer[nameof(AppStrings.SingUpTitle)];
    private string signUp => appLocalizer[nameof(AppStrings.SignUp)];
    private string signIn => appLocalizer[nameof(AppStrings.SignIn)];
    private string signOut => appLocalizer[nameof(AppStrings.SignOut)];
    private string emailTokenConfirmButtonText => appLocalizer[nameof(AppStrings.EmailTokenConfirmButtonText)];
    private string emailTokenPlaceholder => appLocalizer[nameof(AppStrings.EmailTokenPlaceholder)];
    private string defaultFromName => emailLocalizer[nameof(EmailStrings.DefaultFromName)];
    private string confirmationEmailSubject => emailLocalizer[nameof(EmailStrings.ConfirmationEmailSubject)];
    private string welcomeToApp => emailLocalizer[nameof(EmailStrings.WelcomeToApp)];
    private string emailConfirmationMessageSubtitle => emailLocalizer[nameof(EmailStrings.EmailConfirmationMessageSubtitle)];
    private string emailConfirmationMessageBodyToken => emailLocalizer[nameof(EmailStrings.EmailConfirmationMessageBodyToken)];
    private string emailConfirmationMessageBodyLink => emailLocalizer[nameof(EmailStrings.EmailConfirmationMessageBodyLink)];
    #endregion

    public async Task Goto()
    {
        var response = await page.GotoAsync(new Uri(serverAddress, Urls.SignUpPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await page.ChangeCulture(culture);
        await Assertions.Expect(page).ToHaveTitleAsync(singUpTitle);

        //Override behavior of the javascript recaptcha function on the browser
        await page.WaitForFunctionAsync("window.grecaptcha !== undefined");
        await page.EvaluateAsync("window.grecaptcha.getResponse = () => 'not-empty';");
    }

    public async Task SignUp(bool usingMagicLink = true)
    {
        var email = $"{Guid.NewGuid()}@gmail.com";

        await page.GetByPlaceholder(emailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(passwordPlaceholder).FillAsync("password");
        await page.GetByRole(AriaRole.Button, new() { Name = signUp, Exact = true }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = emailTokenConfirmButtonText })).ToBeVisibleAsync();

        var html = ReadEmlAndGetHtmlBody(email);
        var emailPage = await page.Context.NewPageAsync();
        await emailPage.SetContentAsync(html);

        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(welcomeToApp);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(emailConfirmationMessageSubtitle.Replace("{0}", ".*")));
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(emailConfirmationMessageBodyToken);
        await Assertions.Expect(emailPage.GetByRole(AriaRole.Main)).ToContainTextAsync(emailConfirmationMessageBodyLink);

        IPage signedInPage;
        if (usingMagicLink)
        {
            var optLink = emailPage.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ConfirmPage).ToString() });
            await Assertions.Expect(optLink).ToBeVisibleAsync();
            await optLink.ClickAsync();
            signedInPage = emailPage;
        }
        else
        {
            var optCode = await emailPage.GetByText(new Regex("^\\d{6}$")).TextContentAsync();
            await page.GetByPlaceholder(emailTokenPlaceholder).FillAsync(optCode!);
            await page.GetByRole(AriaRole.Button, new() { Name = emailTokenConfirmButtonText }).ClickAsync();
            signedInPage = page;
        }

        await Assertions.Expect(signedInPage).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(signedInPage.Locator(".persona")).ToBeVisibleAsync();
        await Assertions.Expect(signedInPage.Locator(".persona")).ToContainTextAsync(email);
        await Assertions.Expect(signedInPage.GetByRole(AriaRole.Button, new() { Name = signOut })).ToBeVisibleAsync();
        await Assertions.Expect(signedInPage.GetByRole(AriaRole.Button, new() { Name = signIn })).ToBeVisibleAsync(new() { Visible = false });

        await emailPage.CloseAsync();
    }

    private string ReadEmlAndGetHtmlBody(string toMailAddress)
    {
        var email = LoadLastEmailFor(toMailAddress);

        Assert.IsNotNull(email);
        Assert.AreEqual("info@Boilerplate.com", email.Headers.From.Address);
        Assert.AreEqual(defaultFromName, email.Headers.From.DisplayName);
        Assert.IsTrue(Regex.IsMatch(email.Headers.Subject, confirmationEmailSubject.Replace("{0}", "\\d{6}")), "Email subject does not match.");

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
