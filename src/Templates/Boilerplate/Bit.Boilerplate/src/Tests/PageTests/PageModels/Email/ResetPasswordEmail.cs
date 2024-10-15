using System.Text.RegularExpressions;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Tests.PageTests.PageModels.Identity;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests.PageModels.Email;

public partial class ResetPasswordEmail(IBrowserContext context, Uri serverAddress)
{
    private IPage page;
    private const string OpenEmailFirstMessage = $"You must call {nameof(Open)} method first";

    public async Task Open(string emailAddress)
    {
        var html = EmailReaderService.GetLastEmailFor(emailAddress, EmailStrings.ResetPasswordEmailSubject.Replace("{0}", "\\d{6}"));
        page = await context.NewPageAsync();
        await page.SetContentAsync(html);
    }

    public async Task AssertContent()
    {
        Assert.IsNotNull(page, OpenEmailFirstMessage);

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(new Regex(EmailStrings.ResetPasswordTitle.Replace("{0}", ".*")));
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordSubtitle);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordBody);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordTokenMessage);
        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(EmailStrings.ResetPasswordLinkMessage);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ResetPasswordPage).ToString() })).ToBeVisibleAsync();
    }

    public async Task<string> GetToken()
    {
        Assert.IsNotNull(page, OpenEmailFirstMessage);

        var token = await page.GetByText(new Regex("^\\d{6}$")).TextContentAsync();
        Assert.IsNotNull(token, "Confirmation token not found in email");
        return token;
    }

    public async Task<ResetPasswordPage> OpenMagicLink()
    {
        Assert.IsNotNull(page, OpenEmailFirstMessage);

        await page.GetByRole(AriaRole.Link, new() { Name = new Uri(serverAddress, Urls.ResetPasswordPage).ToString() }).ClickAsync();
        return new(page, serverAddress, null);
    }
}
