using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Tests.PageTests.PageModels.Layout;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests.PageModels.Email;

public partial class ConfirmationEmail(IBrowserContext context, Uri serverAddress)
{
    private IPage page;
    private const string OpenEmailFirstMessage = $"You must call {nameof(Open)} method first";

    public async Task Open(string emailAddress)
    {
        var html = EmailReaderService.GetLastEmailFor(emailAddress, EmailStrings.ConfirmationEmailSubject.Replace("{0}", "\\d{6}"));
        page = await context.NewPageAsync();
        await page.SetContentAsync(html);
    }

    public async Task AssertContent()
    {
        Assert.IsNotNull(page, OpenEmailFirstMessage);

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.WelcomeToApp);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(EmailStrings.EmailConfirmationMessageSubtitle.Replace("{0}", ".*")));
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyToken);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.EmailConfirmationMessageBodyLink);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ConfirmPage).ToString() })).ToBeVisibleAsync();
    }

    public async Task<string> GetToken()
    {
        Assert.IsNotNull(page, OpenEmailFirstMessage);

        var token = await page.GetByText(new Regex("^\\d{6}$")).TextContentAsync();
        Assert.IsNotNull(token, "Confirmation token not found in email");
        return token;
    }

    public async Task<IdentityLayout> OpenMagicLink()
    {
        Assert.IsNotNull(page, OpenEmailFirstMessage);

        await page.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ConfirmPage).ToString() }).ClickAsync();
        return new(page, serverAddress, Urls.HomePage, AppStrings.HomeTitle);
    }
}
